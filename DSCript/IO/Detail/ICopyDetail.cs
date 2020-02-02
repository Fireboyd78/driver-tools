namespace DSCript
{
    public interface ICopyDetail<T>
        where T : class
    {
        void CopyTo(T obj);
    }
}
