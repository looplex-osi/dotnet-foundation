using System;

namespace Looplex.Foundation.Entities
{
    public abstract class Actor
    {
        #region Observability

        private event Action<string, object> ActorEvent;

        protected void FireEvent(string eventName, object data = null)
        {
            var handler = ActorEvent; // Prevent potential race conditions on MTA environments
            ActorEvent?.Invoke(eventName, data); // Notify subscribers
        }

        public void AddEventListener(Action<string, object> handler)
        {
            ActorEvent += handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void RemoveEventListener(Action<string, object> handler)
        {
            ActorEvent -= handler ?? throw new ArgumentNullException(nameof(handler));
        }

        #endregion
    }
}