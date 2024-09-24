namespace RemoteControl;

public abstract class RemoteException(string message, Exception? cause = null): ApplicationException(message, cause);

public class NoBrowserConnected(string message): RemoteException(message);