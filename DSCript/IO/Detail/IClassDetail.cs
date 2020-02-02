namespace DSCript
{
    public interface IClassDetail<T>
        where T : class
    {
        T ToClass();
    }
}
