namespace Audiose
{
    public interface ISerializer<T>
    {
        void Serialize(T input);
        void Deserialize(T output);
    }
}
