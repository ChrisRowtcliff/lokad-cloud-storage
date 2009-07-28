﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lokad.Cloud.Framework
{
	/// <summary>Status flag for <see cref="CloudService"/>s.</summary>
	/// <remarks>Starting / stopping services isn't a synchronous operation,
	/// it can take a little while before all the workers notice an update 
	/// on the service state.</remarks>
	[Serializable]
	public enum CloudServiceState
	{
		/// <summary>
		/// Indicates that the service should be running.</summary>
		Started = 0,

		/// <summary>
		/// Indicates that the service should be stopped.
		/// </summary>
		Stopped = 1
	}

	/// <summary>Base class for cloud services.</summary>
	/// <remarks>Do not inherit directly from <see cref="CloudService"/>, inherit from
	/// <see cref="QueueService{T}"/> or <see cref="ScheduledService"/> instead.</remarks>
	public abstract class CloudService
	{
		public const string ServiceStateContainer = "lokad-cloud-services";
		public const string ServiceStatePrefix = "state";
		public const string Delimiter = "/";

		/// <summary>Indicates the state of the service, as retrieved during the last check.</summary>
		CloudServiceState _state = CloudServiceState.Started;

		/// <summary>Indicates the last time the service has checked its excution status.</summary>
		DateTime _lastStateCheck = DateTime.MinValue;

		ProvidersForCloudStorage _providers;

		/// <summary>Indicates the frequency where the service is actually checking for its state.</summary>
		public static TimeSpan StateCheckInterval
		{
			get { return 1.Minutes(); }
		}

		/// <summary>Error logger.</summary>
		public ILog Log
		{
			get { return _providers.Log; }
		}

		/// <summary>Name of the service (used for reporting purposes).</summary>
		/// <remarks>Default implementation returns <c>Type.FullName</c>.</remarks>
		public virtual string Name
		{
			get { return GetType().FullName; }
		}

		/// <summary>Providers used by the cloud service to access the storage.</summary>
		public ProvidersForCloudStorage Providers
		{
			get { return _providers; }
			set { _providers = value; }
		}

		/// <summary>Wrapper method for the <see cref="StartImpl"/> method. Checks
		/// that the service status before executing the inner start.</summary>
		/// <returns></returns>
		public bool Start()
		{
			var now = DateTime.Now;

			// checking service state at regular interval
			if(now.Subtract(_lastStateCheck) > StateCheckInterval)
			{
				var cn = ServiceStateContainer;
				var bn = ServiceStatePrefix + Delimiter + Name;

				var state = _providers.BlobStorage.GetBlob<CloudServiceState?>(cn, bn);

				// no state can be retrieved, update blob storage
				if(!state.HasValue)
				{
					var settings = GetType().GetAttribute<CloudServiceSettingsAttribute>(true);

					state = null != settings ?
							(settings.AutoStart ? CloudServiceState.Started : CloudServiceState.Stopped) :
							CloudServiceState.Started;

					_providers.BlobStorage.PutBlob(cn, bn, state);
				}

				_state = state.Value;
				_lastStateCheck = now;
			}

			// no execution if the service is stopped
			if(CloudServiceState.Stopped == _state)
			{
				return false;
			}
            
			return StartImpl();
		}

		/// <summary>Called when the service is launched.</summary>
		/// <returns><c>true</c> if the service did actually perform an operation, and
		/// <c>false</c> otherwise. This value is used by the framework to adjust the
		/// start frequency of the respective services.</returns>
		/// <remarks>This method is expected to be implemented by the framework services
		/// not by the app services.</remarks>
		protected abstract bool StartImpl();

		/// <summary>Called when the service is shut down.</summary>
		public virtual void Stop()
		{
			// does nothing
		}

		/// <summary>Instanciate a <see cref="BlobSet{T}"/> based on the current
		/// storage providers (prefix is auto-generated based on the type <c>T</c>).</summary>
		public BlobSet<T> GetBlobSet<T>()
		{
			return new BlobSet<T>(_providers, _providers.TypeMapper.GetStorageName(typeof(T)));
		}

		/// <summary>Instanciate a <see cref="BlobSet{T}"/> with the specified prefix name
		/// based on the current storage providers.</summary>
		public BlobSet<T> GetBlobSet<T>(string prefixName)
		{
			return new BlobSet<T>(_providers, prefixName);
		}

		/// <summary>Put messages into the queue implicitely associated to the type <c>T</c>.</summary>
		public void Put<T>(IEnumerable<T> messages)
		{
			Put(messages, _providers.TypeMapper.GetStorageName(typeof(T)));
		}

		/// <summary>Put messages into the queue identified by <c>queueName</c>.</summary>
		public void Put<T>(IEnumerable<T> messages, string queueName)
		{
			_providers.QueueStorage.Put(queueName, messages);
		}

		/// <summary>Get all services instantiated through reflection.</summary>
		internal static IEnumerable<CloudService> GetAllServices(ProvidersForCloudStorage providers)
		{
			// invoking all loaded services through reflexion
			var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
				.Select(a => a.GetExportedTypes()).SelectMany(x => x)
				.Where(t => t.IsSubclassOf(typeof(CloudService)) && !t.IsAbstract && !t.IsGenericType);

			// assuming that a default constructor is available
			var services = serviceTypes.Select(t =>
				(CloudService)t.InvokeMember("_ctor", 
				BindingFlags.CreateInstance, null, null, new object[0]));

			services.ForEach(s => s.Providers = providers);

			return services;
		}
	}
}
