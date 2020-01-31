using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Tools.Productivity.ShortStack
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// A notification for the client
    /// </summary>
    //---------------------------------------------------------------------------------
    [DataContract]
    public class ShortStackNotification
    {
        public enum NotificationStatus
        {
            Detail,
            Information,
            Warning,
            Error
        }

        /// <summary>
        /// A hint for how to display this notification
        /// </summary>
        public NotificationStatus Status { get; set; } = NotificationStatus.Information;

        /// <summary>
        /// Class type for this notification
        /// </summary>
        public string NotificationType { get; set; }

        /// <summary>
        /// Serialized version of the notification object
        /// </summary>
        public string JsonPayload { get; set; }

        /// <summary>
        /// Human-readable text to display
        /// </summary>
        public string DisplayText { get; set; }

        //---------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //---------------------------------------------------------------------------------
        public ShortStackNotification(object notification)
        {
            var notificationType = notification.GetType();
            NotificationType = notificationType.Name;
            var statusProperty = notificationType.GetProperty("Status");
            if(statusProperty != null && Enum.TryParse(statusProperty.GetValue(notification).ToString(), out NotificationStatus status))
            {
                Status = status;
            }
            JsonPayload = JsonConvert.SerializeObject(notification);
        }
    }
}
