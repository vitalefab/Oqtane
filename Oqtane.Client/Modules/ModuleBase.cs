using Microsoft.AspNetCore.Components;
using Oqtane.Shared;
using Oqtane.Models;
using System.Threading.Tasks;
using Oqtane.Services;
using System;
using Oqtane.Enums;
using Oqtane.UI;
using System.Collections.Generic;
using Microsoft.JSInterop;
using System.Linq;
using System.Dynamic;
using System.Reflection;

namespace Oqtane.Modules
{
    public abstract class ModuleBase : ComponentBase, IModuleControl
    {
        private Logger _logger;
        private string _urlparametersstate;
        private Dictionary<string, string> _urlparameters;
        private bool _scriptsloaded = false;

        protected Logger logger => _logger ?? (_logger = new Logger(this));

        [Inject]
        protected ILogService LoggingService { get; set; }

        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected SiteState SiteState { get; set; }

        [CascadingParameter]
        protected PageState PageState { get; set; }

        [CascadingParameter]
        protected Models.Module ModuleState { get; set; }

        [Parameter]
        public RenderModeBoundary RenderModeBoundary { get; set; }

        // optional interface properties
        public virtual SecurityAccessLevel SecurityAccessLevel { get { return SecurityAccessLevel.View; } set { } } // default security

        public virtual string Title { get { return ""; } }

        public virtual string Actions { get { return ""; } }

        public virtual bool UseAdminContainer { get { return true; } }

        public virtual List<Resource> Resources { get; set; }

        public virtual string RenderMode { get { return RenderModes.Interactive; } } // interactive by default

        public virtual bool? Prerender { get { return null; } } // allows the Site Prerender property to be overridden

        // url parameters
        public virtual string UrlParametersTemplate { get; set; }

        public Dictionary<string, string> UrlParameters {
            get
            {
                if (_urlparametersstate == null || _urlparametersstate != PageState.UrlParameters)
                {
                    _urlparametersstate = PageState.UrlParameters;
                    _urlparameters = GetUrlParameters(UrlParametersTemplate);
                }
                return _urlparameters;
            }
        }

        // base lifecycle method for handling JSInterop script registration

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                List<Resource> resources = null;
                var type = GetType();
                if (type.BaseType == typeof(ModuleBase))
                {
                    if (PageState.Page.Resources != null)
                    {
                        resources = PageState.Page.Resources.Where(item => item.ResourceType == ResourceType.Script && item.Level == ResourceLevel.Module && item.Namespace == type.Namespace).ToList();
                    }
                }
                else // modulecontrolbase
                {
                    if (Resources != null)
                    {
                        resources = Resources.Where(item => item.ResourceType == ResourceType.Script).ToList();
                    }
                }
                if (resources != null && resources.Any())
                {
                    var interop = new Interop(JSRuntime);
                    var scripts = new List<object>();
                    var inline = 0;
                    foreach (Resource resource in resources)
                    {
                        if (string.IsNullOrEmpty(resource.RenderMode) || resource.RenderMode == RenderModes.Interactive)
                        {
                            if (!string.IsNullOrEmpty(resource.Url))
                            {
                                var url = (resource.Url.Contains("://")) ? resource.Url : PageState.Alias.BaseUrl + resource.Url;
                                scripts.Add(new { href = url, type = resource.Type ?? "", bundle = resource.Bundle ?? "", integrity = resource.Integrity ?? "", crossorigin = resource.CrossOrigin ?? "", location = resource.Location.ToString().ToLower(), dataAttributes = resource.DataAttributes });
                            }
                            else
                            {
                                inline += 1;
                                await interop.IncludeScript(GetType().Namespace.ToLower() + inline.ToString(), "", "", "", resource.Type ?? "", resource.Content, resource.Location.ToString().ToLower());
                            }
                        }
                    }
                    if (scripts.Any())
                    {
                        await interop.IncludeScripts(scripts.ToArray());
                    }
                }
                _scriptsloaded = true;
            }
        }

        protected override bool ShouldRender()
        {
            return PageState?.RenderId == ModuleState?.RenderId;
        }

        public bool ScriptsLoaded
        {
            get
            {
                return _scriptsloaded;
            }
        }

        // path method

        public string ModulePath()
        {
            return PageState?.Alias.BaseUrl + "/Modules/" + GetType().Namespace + "/";
        }

        // fingerprint hash code for static assets
        public string Fingerprint
        {
            get
            {
                return ModuleState.ModuleDefinition.Fingerprint;
            }
        }

        // url methods

        // navigate url
        public string NavigateUrl()
        {
            return NavigateUrl(PageState.Page.Path);
        }

        public string NavigateUrl(string path)
        {
            return NavigateUrl(path, "");
        }

        public string NavigateUrl(bool refresh)
        {
            return NavigateUrl(PageState.Page.Path, refresh);
        }

        public string NavigateUrl(string path, string querystring)
        {
            return Utilities.NavigateUrl(PageState.Alias.Path, path, querystring);
        }

        public string NavigateUrl(string path, Dictionary<string, string> querystring)
        {
            return NavigateUrl(path, Utilities.CreateQueryString(querystring));
        }

        public string NavigateUrl(string path, bool refresh)
        {
            return NavigateUrl(path, refresh ? "refresh" : "");
        }

        public string NavigateUrl(int moduleId, string action)
        {
            return EditUrl(PageState.Page.Path, moduleId, action, "");
        }

        public string NavigateUrl(int moduleId, string action, string querystring)
        {
            return EditUrl(PageState.Page.Path, moduleId, action, querystring);
        }

        public string NavigateUrl(int moduleId, string action, Dictionary<string, string> querystring)
        {
            return EditUrl(PageState.Page.Path, moduleId, action, querystring);
        }

        public string NavigateUrl(string path, int moduleId, string action)
        {
            return EditUrl(path, moduleId, action, "");
        }

        public string NavigateUrl(string path, int moduleId, string action, string querystring)
        {
            return EditUrl(path, moduleId, action, querystring);
        }

        public string NavigateUrl(string path, int moduleId, string action, Dictionary<string, string> querystring)
        {
            return EditUrl(path, moduleId, action, querystring);
        }

        // edit url
        public string EditUrl(string action)
        {
            return EditUrl(ModuleState.ModuleId, action);
        }

        public string EditUrl(string action, string querystring)
        {
            return EditUrl(ModuleState.ModuleId, action, querystring);
        }

        public string EditUrl(string action, Dictionary<string, string> querystring)
        {
            return EditUrl(ModuleState.ModuleId, action, querystring);
        }

        public string EditUrl(int moduleId, string action)
        {
            return EditUrl(moduleId, action, "");
        }

        public string EditUrl(int moduleId, string action, string querystring)
        {
            return EditUrl(PageState.Page.Path, moduleId, action, querystring);
        }

        public string EditUrl(int moduleId, string action, Dictionary<string, string> querystring)
        {
            return EditUrl(PageState.Page.Path, moduleId, action, querystring);
        }

        public string EditUrl(string path, int moduleid, string action, string querystring)
        {
            return Utilities.EditUrl(PageState.Alias.Path, path, moduleid, action, querystring);
        }

        public string EditUrl(string path, int moduleid, string action, Dictionary<string, string> querystring)
        {
            return EditUrl(path, moduleid, action, Utilities.CreateQueryString(querystring));
        }

        // file url
        public string FileUrl(string folderpath, string filename)
        {
            return FileUrl(folderpath, filename, false);
        }

        public string FileUrl(string folderpath, string filename, bool download)
        {
            return Utilities.FileUrl(PageState.Alias, folderpath, filename, download);
        }
        public string FileUrl(int fileid)
        {
            return FileUrl(fileid, false);
        }

        public string FileUrl(int fileid, bool download)
        {
            return Utilities.FileUrl(PageState.Alias, fileid, download);
        }

        // image url

        public string ImageUrl(int fileid, int width, int height)
        {
            return ImageUrl(fileid, width, height, "");
        }

        public string ImageUrl(int fileid, int width, int height, string mode)
        {
            return ImageUrl(fileid, width, height, mode, "", "", 0, false);
        }

        public string ImageUrl(int fileid, int width, int height, string mode, string position, string background, int rotate, bool recreate)
        {
            return Utilities.ImageUrl(PageState.Alias, fileid, width, height, mode, position, background, rotate, recreate);
        }

        public string AddUrlParameters(params object[] parameters)
        {
            return Utilities.AddUrlParameters(parameters);
        }

        // template is in the form of a standard route template ie. "/{id}/{name}" and produces dictionary of key/value pairs
        // if url parameters belong to a specific module you should embed a unique key into the route (ie. /!/blog/1) and validate the url parameter key in the module
        public virtual Dictionary<string, string> GetUrlParameters(string template = "")
        {
            var urlParameters = new Dictionary<string, string>();
            var parameters = _urlparametersstate.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (string.IsNullOrEmpty(template))
            {
                // no template will populate dictionary with generic "parameter#" keys
                for (int i = 0; i < parameters.Length; i++)
                {
                    urlParameters.TryAdd("parameter" + i, parameters[i]);
                }
            }
            else
            {
                var segments = template.Split('/', StringSplitOptions.RemoveEmptyEntries);
                string key;

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i < segments.Length)
                    {
                        key = segments[i];
                        if (key.StartsWith("{") && key.EndsWith("}"))
                        {
                            // dynamic segment
                            key = key.Substring(1, key.Length - 2);
                        }
                        else
                        {
                            // static segments use generic "parameter#" keys
                            key = "parameter" + i.ToString();
                        }
                    }
                    else // unspecified segments use generic "parameter#" keys
                    {
                        key = "parameter" + i.ToString();
                    }
                    urlParameters.TryAdd(key, parameters[i]);
                }
            }

            return urlParameters;
        }

        // UI methods
        public void AddModuleMessage(string message, MessageType type)
        {
            AddModuleMessage(message, type, "top");
        }

        public void AddModuleMessage(string message, MessageType type, string position)
        {
            RenderModeBoundary.AddModuleMessage(message, type, position);
        }

        public void ClearModuleMessage()
        {
            RenderModeBoundary.AddModuleMessage("", MessageType.Undefined);
        }

        public void ShowProgressIndicator()
        {
            RenderModeBoundary.ShowProgressIndicator();
        }

        public void HideProgressIndicator()
        {
            RenderModeBoundary.HideProgressIndicator();
        }

        public void SetModuleTitle(string title)
        {
            dynamic obj = new ExpandoObject();
            obj.PageModuleId = ModuleState.PageModuleId;
            obj.Title = title;
            SiteState.Properties.ModuleTitle = obj;
        }

        public void SetModuleVisibility(bool visible)
        {
            dynamic obj = new ExpandoObject();
            obj.PageModuleId = ModuleState.PageModuleId;
            obj.Visible = visible;
            SiteState.Properties.ModuleVisibility = obj;
        }

        public void SetPageTitle(string title)
        {
            SiteState.Properties.PageTitle = title;
        }

        // note - only supports links and meta tags - not scripts
        public void AddHeadContent(string content)
        {
            SiteState.AppendHeadContent(content);
        }

        public void AddScript(Resource resource)
        {
            resource.ResourceType = ResourceType.Script;
            if (Resources == null) Resources = new List<Resource>();
            if (!Resources.Any(item => (!string.IsNullOrEmpty(resource.Url) && item.Url == resource.Url) || (!string.IsNullOrEmpty(resource.Content) && item.Content == resource.Content)))
            {
                Resources.Add(resource);
            }
        }

        public async Task ScrollToPageTop()
        {
            var interop = new Interop(JSRuntime);
            await interop.ScrollTo(0, 0, "smooth");
        }

        public string ReplaceTokens(string content)
        {
            return ReplaceTokens(content, null);
        }

        public string ReplaceTokens(string content, object obj)
        {
            var tokens = new List<string>();
            var pos = content.IndexOf("[");
            if (pos != -1)
            {
                if (content.IndexOf("]", pos) != -1)
                {
                    var token = content.Substring(pos, content.IndexOf("]", pos) - pos + 1);
                    if (token.Contains(":"))
                    {
                        tokens.Add(token.Substring(1, token.Length - 2));
                    }
                }
                pos = content.IndexOf("[", pos + 1);
            }
            if (tokens.Count != 0)
            {
                foreach (string token in tokens)
                {
                    var segments = token.Split(":");
                    if (segments.Length >= 2 && segments.Length <= 3)
                    {
                        var objectName = string.Join(":", segments, 0, segments.Length - 1);
                        var propertyName = segments[segments.Length - 1];
                        var propertyValue = "";

                        switch (objectName)
                        {
                            case "ModuleState":
                                propertyValue = ModuleState.GetType().GetProperty(propertyName)?.GetValue(ModuleState, null).ToString();
                                break;
                            case "PageState":
                                propertyValue = PageState.GetType().GetProperty(propertyName)?.GetValue(PageState, null).ToString();
                                break;
                            case "PageState:Alias":
                                propertyValue = PageState.Alias.GetType().GetProperty(propertyName)?.GetValue(PageState.Alias, null).ToString();
                                break;
                            case "PageState:Site":
                                propertyValue = PageState.Site.GetType().GetProperty(propertyName)?.GetValue(PageState.Site, null).ToString();
                                break;
                            case "PageState:Page":
                                propertyValue = PageState.Page.GetType().GetProperty(propertyName)?.GetValue(PageState.Page, null).ToString();
                                break;
                            case "PageState:User":
                                propertyValue = PageState.User?.GetType().GetProperty(propertyName)?.GetValue(PageState.User, null).ToString();
                                break;
                            case "PageState:Route":
                                propertyValue = PageState.Route.GetType().GetProperty(propertyName)?.GetValue(PageState.Route, null).ToString();
                                break;
                            default:
                                if (obj != null && obj.GetType().Name == objectName)
                                {
                                    propertyValue = obj.GetType().GetProperty(propertyName)?.GetValue(obj, null).ToString();
                                }
                                break;
                        }
                        if (propertyValue != null)
                        {
                            content = content.Replace("[" + token + "]", propertyValue);
                        }

                    }
                }
            }
            return content;
        }

        // logging methods
        public async Task Log(Alias alias, LogLevel level, string function, Exception exception, string message, params object[] args)
        {
            LogFunction logFunction;
            if (string.IsNullOrEmpty(function))
            {
                // try to infer from page action
                function = PageState.Action;
            }
            if (!Enum.TryParse(function, out logFunction))
            {
                switch (function.ToLower())
                {
                    case "add":
                        logFunction = LogFunction.Create;
                        break;
                    case "edit":
                        logFunction = LogFunction.Update;
                        break;
                    case "delete":
                        logFunction = LogFunction.Delete;
                        break;
                    case "":
                        logFunction = LogFunction.Read;
                        break;
                    default:
                        logFunction = LogFunction.Other;
                        break;
                }
            }
            await Log(alias, level, logFunction, exception, message, args);
        }

        public async Task Log(Alias alias, LogLevel level, LogFunction function, Exception exception, string message, params object[] args)
        {
            int pageId = ModuleState.PageId;
            int moduleId = ModuleState.ModuleId;
            string category = GetType().AssemblyQualifiedName;
            string feature = Utilities.GetTypeNameLastSegment(category, 1);

            await LoggingService.Log(alias, pageId, moduleId, PageState.User?.UserId, category, feature, function, level, exception, message, args);
        }

        public class Logger
        {
            private readonly ModuleBase _moduleBase;

            public Logger(ModuleBase moduleBase)
            {
                _moduleBase = moduleBase;
            }

            public async Task LogTrace(string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Trace, "", null, message, args);
            }

            public async Task LogTrace(LogFunction function, string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Trace, function, null, message, args);
            }

            public async Task LogTrace(Exception exception, string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Trace, "", exception, message, args);
            }

            public async Task LogDebug(string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Debug, "", null, message, args);
            }

            public async Task LogDebug(LogFunction function, string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Debug, function, null, message, args);
            }

            public async Task LogDebug(Exception exception, string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Debug, "", exception, message, args);
            }

            public async Task LogInformation(string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Information, "", null, message, args);
            }

            public async Task LogInformation(LogFunction function, string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Information, function, null, message, args);
            }

            public async Task LogInformation(Exception exception, string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Information, "", exception, message, args);
            }

            public async Task LogWarning(string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Warning, "", null, message, args);
            }

            public async Task LogWarning(LogFunction function, string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Warning, function, null, message, args);
            }

            public async Task LogWarning(Exception exception, string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Warning, "", exception, message, args);
            }

            public async Task LogError(string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Error, "", null, message, args);
            }

            public async Task LogError(LogFunction function, string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Error, function, null, message, args);
            }

            public async Task LogError(Exception exception, string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Error, "", exception, message, args);
            }

            public async Task LogCritical(string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Critical, "", null, message, args);
            }

            public async Task LogCritical(LogFunction function, string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Critical, function, null, message, args);
            }

            public async Task LogCritical(Exception exception, string message, params object[] args)
            {
                await _moduleBase.Log(null, LogLevel.Critical, "", exception, message, args);
            }
        }

        [Obsolete("ContentUrl(int fileId) is deprecated. Use FileUrl(int fileId) instead.", false)]
        public string ContentUrl(int fileid)
        {
            return ContentUrl(fileid, false);
        }

        [Obsolete("ContentUrl(int fileId, bool asAttachment) is deprecated. Use FileUrl(int fileId, bool download) instead.", false)]
        public string ContentUrl(int fileid, bool asAttachment)
        {
            return Utilities.FileUrl(PageState.Alias, fileid, asAttachment);
        }

        // Referencing ModuleInstance methods from ModuleBase is deprecated. Use the ModuleBase methods instead
        public ModuleInstance ModuleInstance { get { return new ModuleInstance(); } }
    }
}
