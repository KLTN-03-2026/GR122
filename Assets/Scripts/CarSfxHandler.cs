using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class CarSfxHandler : MonoBehaviour
{
    [Header("Mixers")]
    public AudioMixer audioMixer;

    [Header("Audio sources")]
    public AudioSource tiresScreeachingAudioSource;
    public AudioSource engineAudioSource;
    public AudioSource carHitAudioSource;
    public AudioSource carJumpAudioSource;
    public AudioSource carJumpLandingAudioSource;

    //Local variables
    float desiredEnginePitch = 0.5f;
    float tireScreechPitch = 0.5f;

    //Components
    TopDownCarController topDownCarController;

    //Awake is called when the script instance is being loaded.
    void Awake()
    {
        topDownCarController = GetComponentInParent<TopDownCarController>();

        // Cấu hình âm thanh 3D cho AI (không phải player)
        if (!gameObject.CompareTag("Player Racer"))
        {
            Configure3DAudio();
        }
    }

    void Configure3DAudio()
    {
        // Lấy tất cả AudioSource trên GameObject này và con cháu
        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
        foreach (var src in sources)
        {
            src.spatialBlend = 1f;                    // Chế độ 3D
            src.rolloffMode = AudioRolloffMode.Linear; // Giảm tuyến tính về 0 tại maxDistance
            src.minDistance = 7f;                     // Bắt đầu giảm sau 7 đơn vị
            src.maxDistance = 35f;                    // Hoàn toàn im lặng sau 35 đơn vị
            src.dopplerLevel = 0f;                    // Tắt doppler (không cần)
            
            // (Tuỳ chọn) Để giảm đột ngột có thể chỉnh thêm spread
            // src.spread = 30;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //Example for recording, move this part to any setting script that your game might use. 
        //audioMixer.SetFloat("SFXVolume", 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateEngineSFX();
        UpdateTiresScreechingSFX();
    }

    void UpdateEngineSFX()
    {
        //Handle engine SFX
        float velocityMagnitude = topDownCarController.GetVelocityMagnitude();

        //Increase the engine volume as the car goes faster
        float desiredEngineVolume = velocityMagnitude * 0.05f;

        //But keep a minimum level so it playes even if the car is idle
        desiredEngineVolume = Mathf.Clamp(desiredEngineVolume, 0.2f, 1.0f);

        engineAudioSource.volume = Mathf.Lerp(engineAudioSource.volume, desiredEngineVolume, Time.deltaTime * 10);

        //To add more variation to the engine sound we also change the pitch
        desiredEnginePitch = velocityMagnitude * 0.2f;
        desiredEnginePitch = Mathf.Clamp(desiredEnginePitch, 0.5f, 2f);
        engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, desiredEnginePitch, Time.deltaTime * 1.5f);
    }

    void UpdateTiresScreechingSFX()
    {
        //Handle tire screeching SFX
        if (topDownCarController.IsTireScreeching(out float lateralVelocity, out bool isBraking))
        {
            //If the car is braking we want the tire screech to be louder and also change the pitch.
            if (isBraking)
            {
                tiresScreeachingAudioSource.volume = Mathf.Lerp(tiresScreeachingAudioSource.volume, 1.0f, Time.deltaTime * 10);
                tireScreechPitch = Mathf.Lerp(tireScreechPitch, 0.5f, Time.deltaTime * 10);
            }
            else
            {
                //If we are not braking we still want to play this screech sound if the player is drifting.
                tiresScreeachingAudioSource.volume = Mathf.Abs(lateralVelocity) * 0.05f;
                tireScreechPitch = Mathf.Abs(lateralVelocity) * 0.1f;
            }
        }
        //Fade out the tire screech SFX if we are not screeching. 
        else tiresScreeachingAudioSource.volume = Mathf.Lerp(tiresScreeachingAudioSource.volume, 0, Time.deltaTime * 10);
    }

    public void PlayJumpSfx()
    {
        carJumpAudioSource.Play();
    }

    public void PlayLandingSfx()
    {
        carJumpLandingAudioSource.Play();
    }

    void OnCollisionEnter2D(Collision2D collision2D)
    {
        //Get the relative velocity of the collision
        float relativeVelocity = collision2D.relativeVelocity.magnitude;

        float volume = relativeVelocity * 0.1f;

        carHitAudioSource.pitch = Random.Range(0.95f, 1.05f);
        carHitAudioSource.volume = volume;

        if (!carHitAudioSource.isPlaying)
            carHitAudioSource.Play();
    }
}