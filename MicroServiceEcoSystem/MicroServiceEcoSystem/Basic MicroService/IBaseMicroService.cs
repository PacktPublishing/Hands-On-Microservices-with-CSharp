namespace MicroServiceEcoSystem
{
    using Topshelf;

    public interface IBaseMicroService
    {
        bool Start(HostControl hc);
        bool Stop();

    }
}
