using System;

namespace Plex.Contracts
{
    public interface IExec
    {
        void Prepare(string taskName);

        byte[] Function(string key, IStorage storage);

        void CleanUp();
    }
}
