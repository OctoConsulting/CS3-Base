using UnityEngine;

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
using System;
using Windows.Foundation;
using Windows.Devices.Geolocation;
#endif

/// <summary>
/// This class encapsulates the Model of the Geolocation SDK sample app.  Its main goal is to ensure
/// access to geolocation data for the ViewModel.  This class:
/// * Implements the access to source of data, in this case: the Windows.Devices.Geolocation.Geolocator
///   object.
/// * Encapsulates the logic to initialize the Geolocator and check the system access to geolocation
///   information.
/// * Bubbles geolocation position and status change events to the ViewModel.
/// * Catches any exception from the Geolocator and stores the description in the ViewModel so that the
///   View can pick it up and notifiy the user via the UI.
/// </summary>
public class GeolocationSDKModel
{
#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    public uint ReportInterval { get; private set; }
    public double MovementThreshold { get; private set; }

    private Geolocator geolocator;
    private GeolocationAccessStatus geolocationAccessStatus;
    public event TypedEventHandler<Geolocator, StatusChangedEventArgs> GeolocatorStatusChangedEvent;
    public event TypedEventHandler<Geolocator, PositionChangedEventArgs> GeolocatorPositionChangedEvent;
#endif

    /// <summary>
    /// This class is implemented as a thread-safe lazy-instantiated singleton.
    /// </summary>
    GeolocationSDKModel()
    {
        //Empty on purpose
    }
    private static readonly object padLock = new object();
    private static GeolocationSDKModel instance = null;
    public static GeolocationSDKModel Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }
            lock (padLock)
            {
                if (instance == null)
                {
                    instance = new GeolocationSDKModel();
                }
            }
            return instance;
        }
    }

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Gets the Geolocation access status of the device in which the app is running.
    /// </summary>
    /// <returns>GeolocationAccessStatus from Windows.Devices.Geolocation.Geolocator</returns>
    public GeolocationAccessStatus GetGeolocationAccessStatus()
    {
        geolocationAccessStatus = GeolocationAccessStatus.Unspecified;

        try
        {
            geolocationAccessStatus = Geolocator.RequestAccessAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            Debug.LogError("Exception when calling Geolocator.RequestAccessAsync().");
            Debug.LogException(exception);
        }

        return geolocationAccessStatus;
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Attempts to initializat the Geolocator in the device given its parameters.
    /// </summary>
    /// <param name="positionAccuracy">PositionAccuracy value for the Geolocator initialization.</param>
    /// <param name="desiredAccuracyInMeters">The desirect accuracy in meters for the Geolocator initialization.</param>
    /// <returns>True: if the Geolocator was successfully initialized.
    ///          False: if the Geolocator was not initialized.</returns>
    public bool InitializeGeolocator(PositionAccuracy positionAccuracy,
                                     uint desiredAccuracyInMeters)
    {
        if (geolocator == null)
        {
            try
            {
                geolocator = new Geolocator()
                {
                    DesiredAccuracy = positionAccuracy,
                    DesiredAccuracyInMeters = desiredAccuracyInMeters
                };

                ReportInterval = geolocator.ReportInterval;
                MovementThreshold = geolocator.MovementThreshold;

                GeolocationSDKViewModel.Instance.bubbledExceptionMessage = "None, yay! :-)";

                geolocator.StatusChanged += OnStatusChanged;
                geolocator.PositionChanged += OnPositionChanged;
            }
            catch (Exception exception)
            {
                Debug.LogError("Exception during GeolocationSDKModel::InitializeGeolocator");
                Debug.LogException(exception);

                GeolocationSDKViewModel.Instance.bubbledExceptionMessage = exception.Message;

                return false;
            }
            return true;
        }
        else
        {
            Debug.LogWarning("Attempted to initialize Geolocator when it was already initialized.");
            return false;
        }
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Event handler for the Geolocation status change event.  The event is raised to the ViewModel.
    /// </summary>
    /// <param name="sender">The Geolocator event source.</param>
    /// <param name="args">Parameters of the status change event.</param>
    private void OnStatusChanged(Geolocator sender, StatusChangedEventArgs args)
    {
        GeolocatorStatusChangedEvent?.Invoke(sender, args);
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Event handler for the Geolocation position change event.  The event is raised to the ViewModel.
    /// </summary>
    /// <param name="sender">The Geolocator event source.</param>
    /// <param name="args">Parameters of the position change event.</param>
    private void OnPositionChanged(Geolocator sender, PositionChangedEventArgs args)
    {
        GeolocatorPositionChangedEvent?.Invoke(sender, args);
    }
#endif
}
