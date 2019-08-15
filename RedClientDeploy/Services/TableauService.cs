using Newtonsoft.Json;
using RedClientDeploy.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace RedClientDeploy.Services
{

    public class TableauErrors
    {
        public string StatusCode { get; set; }
    }
    public class TableauService
    {
        public HttpClient Client { get; }
        public ServerCredentials LogOn = new ServerCredentials();
        public bool LogOnError { get; private set; }
        public string ErrorMessage { get; private set; }
        public string StatusCode { get; private set; }
        public string ReasonPhrase { get; private set; }
        private readonly string User = Environment.GetEnvironmentVariable("AdminUser");
        private readonly string Password = Environment.GetEnvironmentVariable("AdminPassword");

        public ErrorManager ExecutionIssue { get; private set; }
        public bool HasProblem { get; private set; }

        public TableauService(HttpClient client)
        {
            client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("TableauURL"));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            Client = client;
        }

        private async Task LogOnAsync(string site = "")
        {
            LogOnError = false;

            var j = ToJson((new SignIn(User, Password, site)));

            var request = new HttpRequestMessage(HttpMethod.Post, "3.3/auth/signin")
            {
                Content = new StringContent(j, Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await Client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var l = await response.Content.ReadAsAsync<SignIn>();
                    LogOn = l.Credentials;
                }
                else
                {
                    var error = await response.Content.ReadAsAsync<ErrorDetails>(); ;

                    error.StatusCode = response.StatusCode;
                    error.ReasonPhrase = response.ReasonPhrase;

                    LogOnError = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }

        private async Task SwitchSiteAsync(string site)
        {
            var switchSite = new Site
            {
                SiteDetails = new ServerSite
                {
                    ContentUrl = site
                }
            };

            var j = ToJson(switchSite);

            Client.DefaultRequestHeaders.Remove("X-Tableau-Auth");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tableau-Auth", LogOn.Token);

            var request = new HttpRequestMessage(HttpMethod.Post, "3.3/auth/switchsite")
            {
                Content = new StringContent(j, Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await Client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var l = await response.Content.ReadAsAsync<SignIn>();
                    LogOn = l.Credentials;
                }
                else
                {
                    LogOnError = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }

        private async Task<T> MethodAsync<T>(T content)
        {

            var j = ToJson(content);

            Client.DefaultRequestHeaders.Add("Accept", "application/json");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tableau-Auth", LogOn.Token);

            var request = new HttpRequestMessage(HttpMethod.Post, "3.3/auth/switchsite")
            {
                Content = new StringContent(j, Encoding.UTF8, "application/json")
            };

            var response = await Client.SendAsync(request);

            var r = await response.Content.ReadAsStringAsync();

            T ss = (T)JsonConvert.DeserializeObject(r, typeof(T));

            return ss;

        }

        private async Task<ServerSite> GetSiteAsync(string site)
        {
            LogOnError = false;
            ServerSite serverSite = new ServerSite();
            if (LogOn.Token == null)
            {
                await LogOnAsync(site);
            }
            else if (LogOn.Site.ContentUrl != site)
            {
                await SwitchSiteAsync(site);
            }

            if (LogOnError)
            {
                throw new Exception(ErrorMessage);
            }
            else
            {
                Client.DefaultRequestHeaders.Remove("X-Tableau-Auth");
                Client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tableau-Auth", LogOn.Token);

                var request = new HttpRequestMessage(HttpMethod.Get, $"3.3/sites/{site}?key=contentUrl");

                try
                {
                    using (var response = await Client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var l = await response.Content.ReadAsAsync<Site>();
                            serverSite = l.SiteDetails;
                        }
                        else
                        {
                            LogOnError = true;
                        }
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                request.Dispose();
            }
            return serverSite;
        }

        public async Task<TableauTenant> GetTableauSiteAsync(string site)
        {
            ExecutionIssue.Clear();

            TableauTenant tenant = new TableauTenant();

            var tableauSite = await GetSiteAsync(site);
            var tableauProject = await GetProjectAsync();

            tenant.ProjectId = tableauProject.Id.ToString();
            tenant.SiteId = tableauSite.Id;

            return tenant;
        }

        public async Task<Tenant> NewTableauSiteAsync(string site, string name)
        {
            TableauTenant tenant = new TableauTenant();

            var tableauSite = await CreateSiteAsync(site, name);

            if (!LogOnError)
            {
                var tableauProject = await CreateProjectAsync();

                tenant.ProjectId = tableauProject.Id.ToString();
                tenant.SiteId = tableauSite.Id;
            }

            else
            {

            }


            return new Tenant
            {
                Tableau = tenant,
                Message = ErrorMessage
            };
        }

        private async Task<ProjectDetail> GetProjectAsync()
        {
            ProjectDetail pd = new ProjectDetail();

            var project = WebUtility.UrlEncode(Environment.GetEnvironmentVariable("DefaultProjectName"));

            Client.DefaultRequestHeaders.Remove("X-Tableau-Auth");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tableau-Auth", LogOn.Token);

            string siteId = LogOn.Site.Id;

            var request = new HttpRequestMessage(HttpMethod.Get, $"3.3/sites/{siteId}/projects?filter=name:eq:{project}");

            try
            {
                using (var response = await Client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var l = await response.Content.ReadAsAsync<ProjectResponse>();
                        pd = l.Projects.Project[0];
                    }
                    else
                    {
                        LogOnError = true;
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            request.Dispose();

            return pd;

        }

        private async Task<ServerSite> CreateSiteAsync(string site, string name)
        {
            LogOnError = false;
            ServerSite serverSite = new ServerSite();

            if (LogOn.Token == null)
            {
                await LogOnAsync();
            }

            if (LogOnError)
            {
                throw new Exception(ErrorMessage);
            }

            var newSite = new NewSite
            {
                SiteDetails = new ServerSite
                {
                    Name = name,
                    ContentUrl = site,
                    AdminMode = "ContentOnly"
                }
            };

            string content = ToXML(newSite, "tsRequest");

            Client.DefaultRequestHeaders.Remove("X-Tableau-Auth");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tableau-Auth", LogOn.Token);

            var request = new HttpRequestMessage(HttpMethod.Post, "3.3/sites")
            {
                Content = new StringContent(content, Encoding.UTF8, "application/xml")
            };

            try
            {
                using (var response = await Client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var l = await response.Content.ReadAsAsync<NewSite>();
                        await SwitchSiteAsync(l.SiteDetails.ContentUrl);
                        serverSite = l.SiteDetails;
                    }
                    else
                    {
                        var message = await response.Content.ReadAsStringAsync();
                        ErrorMessage = message;

                        StatusCode = response.StatusCode.ToString();
                        ReasonPhrase = response.ReasonPhrase;
                        LogOnError = true;
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            request.Dispose();

            return serverSite;

        }

        public async Task<Project> CreateProjectAsync()
        {
            Project proj = new Project();

            var project = new NewProject
            {
                ProjectDetails = new Project
                {
                    Name = "RED 2.0 Dashboards"
                }
            };

            string siteId = LogOn.Site.Id;

            string content = ToXML(project, "tsRequest");

            Client.DefaultRequestHeaders.Remove("X-Tableau-Auth");
            Client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tableau-Auth", LogOn.Token);

            var request = new HttpRequestMessage(HttpMethod.Post, $"3.3/sites/{siteId}/projects")
            {
                Content = new StringContent(content, Encoding.UTF8, "application/xml")
            };

            try
            {
                using (var response = await Client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var l = await response.Content.ReadAsAsync<NewProject>();
                        proj = l.ProjectDetails;
                    }
                    else
                    {
                        var message = await response.Content.ReadAsStringAsync();
                        ErrorMessage = message;

                        StatusCode = response.StatusCode.ToString();
                        ReasonPhrase = response.ReasonPhrase;
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            request.Dispose();

            return proj;

        }

        private static string ToJson(object o)
        {
            JsonSerializer jw = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);


            using (JsonWriter wr = new JsonTextWriter(sw))
            {
                jw.Serialize(wr, o);
            }

            return sb.ToString();
        }

        private static string ToXML(object o, string rootName)
        {
            string s = "";
            try
            {
                using (var stringwriter = new System.IO.StringWriter())
                {
                    var ns = new XmlSerializerNamespaces();
                    ns.Add(string.Empty, string.Empty);

                    var serializer = new XmlSerializer(o.GetType(), new XmlRootAttribute { ElementName = rootName });


                    serializer.Serialize(stringwriter, o, ns);

                    XDocument xdoc = XDocument.Parse(stringwriter.ToString());
                    xdoc.Declaration = null;

                    s = xdoc.ToString();
                }

                return s;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }

        }
    }

    public class SignIn
    {
        [XmlElement("credentials"), JsonProperty("credentials")]
        public ServerCredentials Credentials;
        public SignIn(string name, string password, string site)
        {
            Credentials = new ServerCredentials
            {
                Name = name,
                Password = password,
                Site = new ServerSite { ContentUrl = site }
            };

        }

        public SignIn() : base() { }

    }

    public class ServerCredentials
    {
        [XmlAttribute("name"), JsonProperty("name")]
        public string Name;
        [XmlAttribute("password"), JsonProperty("password")]
        public string Password;
        [XmlAttribute("token"), JsonProperty("token")]
        public string Token;
        [XmlElement("site"), JsonProperty("site")]
        public ServerSite Site;
        [XmlElement("user"), JsonProperty("user")]
        public ServerUser User;

        public ServerCredentials(string name, string password, string site)
        {
            Name = name;
            Password = password;
            Site = new ServerSite
            {
                ContentUrl = site
            };
        }

        public ServerCredentials() : base() { }

    }

    [JsonObject("site")]
    public class ServerSite
    {
        [XmlAttribute("contentUrl"), JsonProperty("contentUrl")]
        public string ContentUrl;
        [XmlAttribute("id"), JsonProperty("id")]
        public string Id;
        [XmlAttribute("name"), JsonProperty("name")]
        public string Name { get; set; }
        [XmlAttribute("adminMode"), JsonProperty("adminMode")]
        public string AdminMode { get; set; }
        //[XmlAttribute("disableSubscriptions"), JsonProperty("disableSubscriptions")]
        //public bool DisableSubscriptions { get; set; } = false;
        //[XmlAttribute("state"), JsonProperty("state")]
        //public string State { get; set; }
        //[XmlAttribute("revisionHistoryEnabled"), JsonProperty("revisionHistoryEnabled")]
        //public bool RevisionHistoryEnabled { get; set; } = false;
        //[XmlAttribute("revisionLimit"), JsonProperty("revisionLimit")]
        //public string RevisionLimit { get; set; }
        //[XmlAttribute("subscribeOthersEnabled"), JsonProperty("subscribeOthersEnabled")]
        //public bool SubscribeOthersEnabled { get; set; } = true;
        //[XmlAttribute("guestAccessEnabled"), JsonProperty("guestAccessEnabled")]
        //public bool GuestAccessEnabled { get; set; } = false;
        //[XmlAttribute("cacheWarmupEnabled"), JsonProperty("cacheWarmupEnabled")]
        //public bool CacheWarmupEnabled { get; set; } = false;
        //[XmlAttribute("commentingEnabled"), JsonProperty("commentingEnabled")]
        //public bool CommentingEnabled { get; set; } = true;
        //[XmlAttribute("flowsEnabled"), JsonProperty("flowsEnabled")]
        //public bool FlowsEnabled { get; set; } = true;
        //[XmlAttribute("extractEncryptionMode"), JsonProperty("extractEncryptionMode")]
        //public string ExtractEncryptionMode { get; set; }


    }

    public class ServerUser
    {
        [XmlAttribute("name"), JsonProperty("name")]
        public string Name { get; set; }
        [XmlAttribute("siteRole"), JsonProperty("siteRole")]
        public string SiteRole { get; set; } = "Explorer";
        [XmlAttribute("authSetting"), JsonProperty("authSetting")]
        public string AuthSetting { get; set; }
    }

    public class Site
    {
        [XmlElement("site"), JsonProperty("site")]
        public ServerSite SiteDetails { get; set; }
    }

    public partial class ProjectResponse
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("projects")]
        public Projects Projects { get; set; }
    }

    public partial class Pagination
    {
        [JsonProperty("pageNumber")]

        public long PageNumber { get; set; }

        [JsonProperty("pageSize")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long PageSize { get; set; }

        [JsonProperty("totalAvailable")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long TotalAvailable { get; set; }
    }

    public partial class Projects
    {
        [JsonProperty("project")]
        public ProjectDetail[] Project { get; set; }
    }

    public partial class ProjectDetail
    {
        [JsonProperty("owner")]
        public Owner Owner { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonProperty("contentPermissions")]
        public string ContentPermissions { get; set; }
    }

    public partial class Owner
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }

    public class NewSite
    {
        [XmlElement("site"), JsonProperty("site")]
        public ServerSite SiteDetails { get; set; }
    }

    public class NewProject
    {
        [XmlElement("project"), JsonProperty("project")]
        public Project ProjectDetails { get; set; }
    }

    public class Project
    {
        [XmlAttribute("parentProjectId"), JsonProperty("parentProjectId")]
        public string ParentId { get; set; }
        [XmlAttribute("name"), JsonProperty("name")]
        public string Name { get; set; }
        [XmlAttribute("description"), JsonProperty("description")]
        public string Description { get; set; }
        [XmlAttribute("contentPermissions"), JsonProperty("contentPermissions")]
        public string ContentPermissions { get; set; } = "LockedToProject";
        [XmlAttribute("id"), JsonProperty("id")]
        public string Id { get; set; }
        [XmlAttribute("createdAt"), JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
        [XmlAttribute("updatedAt"), JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }
        [XmlElement("owner"), JsonProperty("owner")]
        public ProjectOwner Owner { get; set; }
    }

    [JsonObject("owner")]
    public class ProjectOwner
    {
        public string Id { get; set; }
    }
}
