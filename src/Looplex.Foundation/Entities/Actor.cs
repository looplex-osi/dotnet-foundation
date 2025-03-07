using System;

namespace Looplex.Foundation.Entities {
  public abstract class Actor
  {
    #region Reflectivity
    /* NOTE: Normally, since abstract classes can’t be instantiated directly,
    ReSharper might flag a public constructor as unnecessary or potentially
    indicative of a design flaw. However, there are cases—like using reflection
    or providing a clear initialization path for derived classes—where a public
    constructor is intentional. By adding this comment, you're telling ReSharper:
    "I know what I'm doing here; don't warn me about this one time."
    */
    // ReSharper disable once PublicConstructorInAbstractClass
    public Actor() { }
    #endregion

    #region Observability
    private event Action<string, object> ActorEvent;
    protected void FireEvent(string eventName, object data = null)
    {
      Action<string, object> handler = ActorEvent; // Prevent potential race conditions on MTA environments
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
