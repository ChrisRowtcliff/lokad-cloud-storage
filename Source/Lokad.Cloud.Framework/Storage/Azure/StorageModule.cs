﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Net;
using Autofac;
using Lokad.Cloud.Storage.Shared;
using Lokad.Cloud.Management;
using Lokad.Cloud.Runtime;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Storage.Azure
{
    /// <summary>IoC module that registers
    /// <see cref="BlobStorageProvider"/>, <see cref="QueueStorageProvider"/> and
    /// <see cref="TableStorageProvider"/> from the <see cref="ICloudConfigurationSettings"/>.</summary>
    public sealed class StorageModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CloudFormatter>().As<IDataSerializer>().PreserveExistingDefaults();

            builder.Register(StorageAccountFromSettings);
            builder.Register(QueueClient);
            builder.Register(BlobClient);
            builder.Register(TableClient);

            builder.Register(BlobStorageProvider);
            builder.Register(QueueStorageProvider);
            builder.Register(TableStorageProvider);

            builder.Register(RuntimeProviders);
            builder.Register(CloudStorageProviders);
            builder.Register(CloudInfrastructureProviders);
        }

        private static CloudStorageAccount StorageAccountFromSettings(IComponentContext c)
        {
            var settings = c.Resolve<ICloudConfigurationSettings>();
            CloudStorageAccount account;
            if (CloudStorageAccount.TryParse(settings.DataConnectionString, out account))
            {
                // http://blogs.msdn.com/b/windowsazurestorage/archive/2010/06/25/nagle-s-algorithm-is-not-friendly-towards-small-requests.aspx
                ServicePointManager.FindServicePoint(account.BlobEndpoint).UseNagleAlgorithm = false;
                ServicePointManager.FindServicePoint(account.TableEndpoint).UseNagleAlgorithm = false;
                ServicePointManager.FindServicePoint(account.QueueEndpoint).UseNagleAlgorithm = false;

                return account;
            }
            throw new InvalidOperationException("Failed to get valid connection string");
        }

        static RuntimeProviders RuntimeProviders(IComponentContext c)
        {
            return CloudStorage
                .ForAzureAccount(c.Resolve<CloudStorageAccount>())
                .BuildRuntimeProviders();
        }

        static CloudInfrastructureProviders CloudInfrastructureProviders(IComponentContext c)
        {
            return new CloudInfrastructureProviders(
                c.Resolve<CloudStorageProviders>(),
                c.ResolveOptional<IProvisioningProvider>());
        }

        static CloudStorageProviders CloudStorageProviders(IComponentContext c)
        {
            return new CloudStorageProviders(
                c.Resolve<IBlobStorageProvider>(),
                c.Resolve<IQueueStorageProvider>(),
                c.Resolve<ITableStorageProvider>(),
                c.ResolveOptional<IRuntimeFinalizer>(),
                c.ResolveOptional<Shared.Logging.ILog>());
        }

        static ITableStorageProvider TableStorageProvider(IComponentContext c)
        {
            IDataSerializer formatter;
            if (!c.TryResolve(out formatter))
            {
                formatter = new CloudFormatter();
            }

            return new TableStorageProvider(
                c.Resolve<CloudTableClient>(),
                formatter,
                c.ResolveOptional<Events.Observers.ICloudStorageObserver>());
        }

        static IQueueStorageProvider QueueStorageProvider(IComponentContext c)
        {
            IDataSerializer formatter;
            if (!c.TryResolve(out formatter))
            {
                formatter = new CloudFormatter();
            }

            return new QueueStorageProvider(
                c.Resolve<CloudQueueClient>(),
                c.Resolve<IBlobStorageProvider>(),
                formatter,
                // RuntimeFinalizer is a dependency (as the name suggest) on the worker runtime
                // This dependency is typically not available in a pure O/C mapper scenario.
                // In such case, we just pass a dummy finalizer (that won't be used any
                c.ResolveOptional<IRuntimeFinalizer>(),
                c.ResolveOptional<Events.Observers.ICloudStorageObserver>(),
                c.ResolveOptional<Shared.Logging.ILog>());
        }

        static IBlobStorageProvider BlobStorageProvider(IComponentContext c)
        {
            IDataSerializer formatter;
            if (!c.TryResolve(out formatter))
            {
                formatter = new CloudFormatter();
            }

            return new BlobStorageProvider(
                c.Resolve<CloudBlobClient>(),
                formatter,
                c.ResolveOptional<Events.Observers.ICloudStorageObserver>(),
                c.ResolveOptional<Shared.Logging.ILog>());
        }

        static CloudTableClient TableClient(IComponentContext c)
        {
            var account = c.Resolve<CloudStorageAccount>();
            var storage = account.CreateCloudTableClient();
            storage.RetryPolicy = BuildDefaultRetry();
            return storage;
        }

        static CloudBlobClient BlobClient(IComponentContext c)
        {
            var account = c.Resolve<CloudStorageAccount>();
            var storage = account.CreateCloudBlobClient();
            storage.RetryPolicy = BuildDefaultRetry();
            return storage;
        }

        static CloudQueueClient QueueClient(IComponentContext c)
        {
            var account = c.Resolve<CloudStorageAccount>();
            var queueService = account.CreateCloudQueueClient();
            queueService.RetryPolicy = BuildDefaultRetry();
            return queueService;
        }

        static RetryPolicy BuildDefaultRetry()
        {
            // [abdullin]: in short this gives us MinBackOff + 2^(10)*Rand.(~0.5.Seconds())
            // at the last retry. Reflect the method for more details
            var deltaBackoff = TimeSpan.FromSeconds(0.5);
            return RetryPolicies.RetryExponential(10, deltaBackoff);
        }
    }
}