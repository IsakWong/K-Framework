public interface IDataContainer
{
    bool GetData<T>(string name, out T t) where T : class;
    bool HasData<T>(string name);

    bool HasKey(string name);
    void SetData<T>(string name, T t);

}
/// <summary>
/// 这是一个单例类，不同于KSystem可以装卸，是无法动态卸载的，常用于 Log，广播等
/// </summary>
/// <typeparam name="T"></typeparam>
public class KSingleton<T> where T : KSingleton<T>, new()
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new T();
            }
            return instance;
        }
    }

    protected KSingleton()
    {
        // Do nothing
    }
}