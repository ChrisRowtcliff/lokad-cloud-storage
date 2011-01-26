using Lokad.Cloud.Storage;
using Lokad.Quality;

namespace Lokad.Cloud.Runtime
{
    public class RuntimeProviders
    {
        /// <summary>Abstracts the Blob Storage.</summary>
        public IBlobStorageProvider BlobStorage { get; private set; }

        /// <summary>Abstracts the Queue Storage.</summary>
        public IQueueStorageProvider QueueStorage { get; private set; }

        /// <summary>Abstracts the Table Storage.</summary>
        public ITableStorageProvider TableStorage { get; private set; }

        /// <summary>Abstracts the finalizer (used for fast resource release
        /// in case of runtime shutdown).</summary>
        public IRuntimeFinalizer RuntimeFinalizer { get; private set; }

        public ILog Log { get; private set; }

        /// <summary>IoC constructor.</summary>
        public RuntimeProviders(
            [NotNull] IBlobStorageProvider blobStorage,
            [NotNull] IQueueStorageProvider queueStorage,
            [NotNull] ITableStorageProvider tableStorage,
            IRuntimeFinalizer runtimeFinalizer,
            ILog log)
        {
            BlobStorage = blobStorage;
            QueueStorage = queueStorage;
            TableStorage = tableStorage;
            RuntimeFinalizer = runtimeFinalizer;
            Log = log;
        }
    }
}