using UnityEngine;

public class SimpleWaterAnim : MonoBehaviour
{
    public Sprite[] frames; // 2 спрайта в инспекторе
    public float fps = 4f;

    private SpriteRenderer sr;
    private float timer;
    private int currentFrame;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (frames.Length > 0)
        {
            sr.sprite = frames[0];
        }

        // Случайный старт для разнообразия
        timer = Random.Range(0f, 1f / fps);
    }

    void Update()
    {
        if (frames.Length < 2) return; // Нужно минимум 2 спрайта

        timer += Time.deltaTime;

        if (timer >= 1f / fps)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            sr.sprite = frames[currentFrame];
        }
    }
}