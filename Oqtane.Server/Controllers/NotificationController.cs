using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Oqtane.Enums;
using Oqtane.Models;
using Oqtane.Shared;
using Oqtane.Infrastructure;
using Oqtane.Repository;
using Oqtane.Security;
using System.Net;

namespace Oqtane.Controllers
{
    [Route(ControllerRoutes.ApiRoute)]
    public class NotificationController : Controller
    {
        private readonly INotificationRepository _notifications;
        private readonly IUserPermissions _userPermissions;
        private readonly ISyncManager _syncManager;
        private readonly ILogManager _logger;
        private readonly Alias _alias;

        public NotificationController(INotificationRepository notifications, IUserPermissions userPermissions, ISyncManager syncManager, ILogManager logger, ITenantManager tenantManager)
        {
            _notifications = notifications;
            _userPermissions = userPermissions;
            _syncManager = syncManager;
            _logger = logger;
            _alias = tenantManager.GetAlias();
        }

        // GET: api/<controller>/read?siteid=x&direction=to&userid=1&count=5&isread=false
        [HttpGet("read")]
        [Authorize(Roles = RoleNames.Registered)]
        public IEnumerable<Notification> Get(string siteid, string direction, string userid, string count, string isread)
        {
            IEnumerable<Notification> notifications = null;

            int SiteId;
            int UserId;
            int Count;
            bool IsRead;
            if (int.TryParse(siteid, out SiteId) && SiteId == _alias.SiteId && int.TryParse(userid, out UserId) && int.TryParse(count, out Count) && bool.TryParse(isread, out IsRead) && IsAuthorized(UserId))
            {
                if (direction == "to")
                {
                    notifications = _notifications.GetNotifications(SiteId, -1, UserId, Count, IsRead);
                }
                else
                {
                    notifications = _notifications.GetNotifications(SiteId, UserId, -1, Count, IsRead);
                }
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Notification Get Attempt {SiteId} {Direction} {UserId} {Count} {isRead}", siteid, direction, userid, count, isread);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                notifications = null;
            }


            return notifications;
        }

        // GET: api/<controller>/read?siteid=x&direction=to&userid=1&count=5&isread=false
        [HttpGet("read-count")]
        [Authorize(Roles = RoleNames.Registered)]
        public int Get(string siteid, string direction, string userid, string isread)
        {
            int notificationsCount = 0;

            int SiteId;
            int UserId;
            bool IsRead;
            if (int.TryParse(siteid, out SiteId) && SiteId == _alias.SiteId && int.TryParse(userid, out UserId) && bool.TryParse(isread, out IsRead) && IsAuthorized(UserId))
            {
                if (direction == "to")
                {
                    notificationsCount = _notifications.GetNotificationCount(SiteId, -1, UserId, IsRead);
                }
                else
                {
                    notificationsCount = _notifications.GetNotificationCount(SiteId, UserId, -1, IsRead);
                }
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Notification Get Attempt {SiteId} {Direction} {UserId} {isRead}", siteid, direction, userid, isread);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                notificationsCount = 0;
            }


            return notificationsCount;
        }


        // GET: api/<controller>?siteid=x&type=y&userid=z
        [HttpGet]
        [Authorize(Roles = RoleNames.Registered)]
        public IEnumerable<Notification> Get(string siteid, string direction, string userid)
        {
            IEnumerable<Notification> notifications = null;

            int SiteId;
            int UserId;
            if (int.TryParse(siteid, out SiteId) && SiteId == _alias.SiteId && int.TryParse(userid, out UserId) && IsAuthorized(UserId))
            {
                if (direction == "to")
                {
                    notifications = _notifications.GetNotifications(SiteId, -1, UserId);
                }
                else
                {
                    notifications = _notifications.GetNotifications(SiteId, UserId, -1);
                }
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Notification Get Attempt {SiteId} {Direction} {UserId}", siteid, direction, userid);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                notifications = null;
            }

            return notifications;
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        [Authorize(Roles = RoleNames.Registered)]
        public Notification Get(int id)
        {
            Notification notification = _notifications.GetNotification(id);
            if (notification != null && notification.SiteId == _alias.SiteId && (IsAuthorized(notification.FromUserId) || IsAuthorized(notification.ToUserId)))
            {
                return notification;
            }
            else
            {
                if (notification != null)
                {
                    _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Notification Get Attempt {NotificationId}", id);
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                }
                else
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
                return null;
            }
        }

        // POST api/<controller>
        [HttpPost]
        [Authorize(Roles = RoleNames.Registered)]
        public Notification Post([FromBody] Notification notification)
        {
            if (ModelState.IsValid && notification.SiteId == _alias.SiteId && (IsAuthorized(notification.FromUserId) || (notification.FromUserId == null && User.IsInRole(RoleNames.Admin))))
            {
                if (!User.IsInRole(RoleNames.Admin))
                {
                    // content must be HTML encoded for non-admins to prevent HTML injection
                    notification.Subject = WebUtility.HtmlEncode(notification.Subject);
                    notification.Body = WebUtility.HtmlEncode(notification.Body);
                }
                notification = _notifications.AddNotification(notification);
                _syncManager.AddSyncEvent(_alias, EntityNames.Notification, notification.NotificationId, SyncEventActions.Create);
                _logger.Log(LogLevel.Information, this, LogFunction.Create, "Notification Added {NotificationId}", notification.NotificationId);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Notification Post Attempt {Notification}", notification);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                notification = null;
            }
            return notification;
        }

        // PUT api/<controller>/5
        [HttpPut("{id}")]
        [Authorize(Roles = RoleNames.Registered)]
        public Notification Put(int id, [FromBody] Notification notification)
        {
            if (ModelState.IsValid && notification.SiteId == _alias.SiteId && notification.NotificationId == id && _notifications.GetNotification(notification.NotificationId, false) != null)
            {
                bool update = false;
                if (IsAuthorized(notification.FromUserId))
                {
                    // notification belongs to current authenticated user - update is allowed
                    if (!User.IsInRole(RoleNames.Admin))
                    {
                        // content must be HTML encoded for non-admins to prevent HTML injection
                        notification.Subject = WebUtility.HtmlEncode(notification.Subject);
                        notification.Body = WebUtility.HtmlEncode(notification.Body);
                    }
                    update = true;
                }
                else
                {
                    if (IsAuthorized(notification.ToUserId))
                    {
                        // notification was sent to current authenticated user - only isread and isdeleted properties can be updated
                        var isread = notification.IsRead;
                        var isdeleted = notification.IsDeleted;
                        notification = _notifications.GetNotification(notification.NotificationId);
                        notification.IsRead = isread;
                        notification.IsDeleted = isdeleted;
                        update = true;
                    }
                }
                if (update)
                {
                    notification = _notifications.UpdateNotification(notification);
                    _syncManager.AddSyncEvent(_alias, EntityNames.Notification, notification.NotificationId, SyncEventActions.Update);
                    _logger.Log(LogLevel.Information, this, LogFunction.Update, "Notification Updated {NotificationId}", notification.NotificationId);
                }
                else
                {
                    _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Notification Put Attempt {Notification}", notification);
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    notification = null;
                }
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Notification Put Attempt {Notification}", notification);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                notification = null;
            }
            return notification;
        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}")]
        [Authorize(Roles = RoleNames.Registered)]
        public void Delete(int id)
        {
            Notification notification = _notifications.GetNotification(id);
            if (notification != null && notification.SiteId == _alias.SiteId && (IsAuthorized(notification.FromUserId) || IsAuthorized(notification.ToUserId)))
            {
                _notifications.DeleteNotification(id);
                _syncManager.AddSyncEvent(_alias, EntityNames.Notification, notification.NotificationId, SyncEventActions.Delete);
                _logger.Log(LogLevel.Information, this, LogFunction.Delete, "Notification Deleted {NotificationId}", id);
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Notification Delete Attempt {NotificationId}", id);
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
        }

        private bool IsAuthorized(int? userid)
        {
            bool authorized = false;
            if (userid != null)
            {
                authorized = (_userPermissions.GetUser(User).UserId == userid);
            }
            return authorized;
        }

    }
}
