using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Threading;

namespace Plex.Helpers
{
    public static class CompositionHost
    {
        public static CompositionContainer Container { get; private set; }

        public static void Initialize(CompositionContainer container)
        {
            CompositionContainer globalContainer;
            TryGetOrCreateContainer(() => container, out globalContainer);
        }

        public static void BuildUp(this CompositionContainer container, object obj)
        {
            var batch = new CompositionBatch();
            var part = AttributedModelServices.CreatePart(obj);

            if (part.ImportDefinitions.Any())
                batch.AddPart(part);

            container.Compose(batch);
        }

        private static void TryGetOrCreateContainer(Func<CompositionContainer> createContainer, out CompositionContainer globalContainer)
        {
            if (Container == null)
            {
                var container = createContainer();
                lock (LockObject)
                {
                    if (Container == null)
                    {
                        Thread.MemoryBarrier();
                        Container = container;
                    }
                }
            }

            globalContainer = Container;
        }

        private static readonly object LockObject = new object();
    }
}