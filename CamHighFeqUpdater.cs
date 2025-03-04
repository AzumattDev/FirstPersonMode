/*using System.Diagnostics;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace FirstPersonMode;

/// <summary>
/// This component runs an internal thread to compute smooth camera values at a higher rate than the frame rate.
/// Call UpdateTarget() from the main thread to set a new desired position and rotation.
/// Then, GetSmoothedTransform() returns the interpolated values computed in the background.
/// </summary>
public class CameraHighFrequencyUpdater : MonoBehaviour
{
    public static CameraHighFrequencyUpdater Instance { get; private set; }

    // These variables hold the current smoothed camera transform.
    private Vector3 _smoothedPosition;
    private Quaternion _smoothedRotation;

    private Thread _updateThread;
    private bool _running;
    private readonly object _lock = new object();
    private float _updateRateHz = 200f;
    //private RefreshRate refreshRate = Screen.currentResolution.refreshRateRatio;


    // Variables for interpolation
    private Vector3 _previousTargetPosition;
    private Vector3 _currentTargetPosition;
    private Quaternion _previousTargetRotation;
    private Quaternion _currentTargetRotation;
    private float _alpha;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        // Initialize with the current transform values.
        _smoothedPosition = transform.position;
        _smoothedRotation = transform.rotation;
        _previousTargetPosition = _smoothedPosition;
        _currentTargetPosition = _smoothedPosition;
        _previousTargetRotation = _smoothedRotation;
        _currentTargetRotation = _smoothedRotation;
        _alpha = 1f; // Start fully at the target.

        StartUpdater();
    }

    void OnDestroy()
    {
        StopUpdater();
    }

    /// <summary>
    /// Called on the main thread when a new target camera transform is available.
    /// This resets the interpolation.
    /// </summary>
    public void UpdateTarget(Vector3 targetPosition, Quaternion targetRotation)
    {
        lock (_lock)
        {
            // Begin a new interpolation cycle.
            _previousTargetPosition = _smoothedPosition;
            _currentTargetPosition = targetPosition;
            _previousTargetRotation = _smoothedRotation;
            _currentTargetRotation = targetRotation;
            _alpha = 0f;
        }
    }

    /// <summary>
    /// Returns the current smoothed camera position and rotation.
    /// </summary>
    public (Vector3, Quaternion) GetSmoothedTransform()
    {
        lock (_lock)
        {
            return (_smoothedPosition, _smoothedRotation);
        }
    }

    private void StartUpdater()
    {
        if (_running)
            return;
        _running = true;
        _updateThread = new Thread(UpdateLoop)
        {
            IsBackground = true
        };
        _updateThread.Start();
    }

    private void StopUpdater()
    {
        _running = false;
        _updateThread?.Join();
    }

    private void UpdateLoop()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        double tickInterval = Stopwatch.Frequency / _updateRateHz;
        //double tickInterval = Stopwatch.Frequency / refreshRate.value;
        long lastTick = stopwatch.ElapsedTicks;

        while (_running)
        {
            long now = stopwatch.ElapsedTicks;
            if (now - lastTick >= tickInterval)
            {
                lock (_lock)
                {
                    // Compute how much of the interpolation to advance.
                    float deltaAlpha = (float)((now - lastTick) / (double)Stopwatch.Frequency * _updateRateHz);
                    //float deltaAlpha = (float)((now - lastTick) / (double)Stopwatch.Frequency * refreshRate.value);
                    _alpha = Mathf.Clamp01(_alpha + deltaAlpha);

                    // Lerp position and slerp rotation between previous and current targets.
                    _smoothedPosition = Vector3.Lerp(_previousTargetPosition, _currentTargetPosition, _alpha);
                    _smoothedRotation = Quaternion.Slerp(_previousTargetRotation, _currentTargetRotation, _alpha);
                }

                lastTick = now;
            }

            Thread.Sleep(1);
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
static class PlayerSetLocalPlayerPatch
{
    static void Postfix(Player __instance)
    {
        if (FirstPersonModePlugin.Instance == null) return;
        if (CameraHighFrequencyUpdater.Instance == null)
        {
            __instance.gameObject.AddComponent<CameraHighFrequencyUpdater>();
        }
    }
}*/