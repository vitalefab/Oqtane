using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Oqtane.Models;
using Oqtane.Repository;
using Oqtane.Shared;

namespace Oqtane.Infrastructure
{
    public class TenantManager : ITenantManager
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAliasRepository _aliasRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly SiteState _siteState;

        public TenantManager(IHttpContextAccessor httpContextAccessor, IAliasRepository aliasRepository, ITenantRepository tenantRepository, SiteState siteState)
        {
            _httpContextAccessor = httpContextAccessor;
            _aliasRepository = aliasRepository;
            _tenantRepository = tenantRepository;
            _siteState = siteState;
        }

        public Alias GetAlias()
        {
            Alias alias = null;

            // does not support mock Alias objects (GetTenant should be used to retrieve a TenantId)
            if (_siteState?.Alias != null && _siteState.Alias.AliasId != -1) 
            {
                alias = _siteState.Alias;
            }
            else
            {
                // if there is HttpContext
                var httpcontext = _httpContextAccessor.HttpContext;
                if (httpcontext != null)
                {
                    // legacy support for client api requests which would include the alias as a path prefix ( ie. {alias}/api/[controller] )
                    int aliasId;
                    string[] segments = httpcontext.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (segments.Length > 1 && Shared.Constants.ReservedRoutes.Contains(segments[1]) && int.TryParse(segments[0], out aliasId))
                    {
                        alias = _aliasRepository.GetAliases().ToList().FirstOrDefault(item => item.AliasId == aliasId);
                    }

                    // resolve alias based on host name and path
                    if (alias == null)
                    {
                        string name = httpcontext.Request.Host.Value + httpcontext.Request.Path;
                        alias = _aliasRepository.GetAlias(name);
                    }

                    // if there is a match save it
                    if (alias != null)
                    {
                        alias.Protocol = (httpcontext.Request.IsHttps) ? "https://" : "http://";
                        alias.BaseUrl = "";
                        if (httpcontext.Request.Headers.ContainsKey("User-Agent") && httpcontext.Request.Headers["User-Agent"] == Shared.Constants.MauiUserAgent)
                        {
                            alias.BaseUrl = alias.Protocol + alias.Name.Replace("/" + alias.Path, "");
                        }
                        _siteState.Alias = alias;
                    }
                }
            }

            return alias;
        }

        public Tenant GetTenant()
        {
            var alias = _siteState?.Alias;
            if (alias != null)
            {
                return _tenantRepository.GetTenant(alias.TenantId);
            }
            return null;
        }

        // background processes can set the alias using the SiteState service
        public void SetAlias(Alias alias)
        {
            _siteState.Alias = alias;
        }

        public void SetAlias(int tenantId, int siteId)
        {
            _siteState.Alias = _aliasRepository.GetAliases().ToList().FirstOrDefault(item => item.TenantId == tenantId && item.SiteId == siteId);
        }

        public void SetTenant(int tenantId)
        {
            _siteState.Alias = new Alias { TenantId = tenantId, AliasId = -1, SiteId = -1 };
        }
    }
}
