﻿using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media;
using Windows.Storage.Streams;
namespace MauiAudio;

public class NativeAudioService : INativeAudioService
{
    string _uri;
    MediaPlayer mediaPlayer;

    public bool IsPlaying => mediaPlayer != null
        && mediaPlayer.CurrentState == MediaPlayerState.Playing;
    public double Duration => mediaPlayer?.NaturalDuration.TotalSeconds ?? 0;
    public double CurrentPosition => mediaPlayer?.Position.TotalSeconds ?? 0;
    public event EventHandler<bool> IsPlayingChanged;
    public event EventHandler PlayEnded;
    public event EventHandler PlayNext;
    public event EventHandler PlayPrevious;

    public async Task InitializeAsync(string audioURI)
    {
        await InitializeAsync(new MediaPlay() { URL = audioURI });
    }

    public Task PauseAsync()
    {
        mediaPlayer?.Pause();
        return Task.CompletedTask;
    }

    public Task PlayAsync(double position = 0)
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Position = TimeSpan.FromSeconds(position);
            mediaPlayer.Play();
        }

        return Task.CompletedTask;
    }

    public Task SetCurrentTime(double value)
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Position = TimeSpan.FromSeconds(value);
        }
        return Task.CompletedTask;
    }

    public Task SetMuted(bool value)
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.IsMuted = value;
        }
        return Task.CompletedTask;
    }

    public Task SetVolume(int value)
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Volume = value != 0 ? value / 100d : 0;
        }

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        mediaPlayer?.Dispose();
        return Task.CompletedTask;
    }
    private MediaPlaybackItem mediaPlaybackItem(MediaPlay media)
    {
        var mediaItem = new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri(media.URL)));
        var props = mediaItem.GetDisplayProperties();
        props.Type = MediaPlaybackType.Music;
        if (media.Name != null) props.MusicProperties.Title = media.Name;
        if (media.Author != null) props.MusicProperties.Artist = media.Author;
        if (media.Image != null)
            props.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(media.Image));
        mediaItem.ApplyDisplayProperties(props);
        return mediaItem;
    }
    public async Task InitializeAsync(MediaPlay media)
    {
        _uri = media.URL;

        if (mediaPlayer == null)
        {
            mediaPlayer = new MediaPlayer
            {
                Source = mediaPlaybackItem(media),
                AudioCategory = MediaPlayerAudioCategory.Media
            };
            mediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;
            mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
            mediaPlayer.CommandManager.PauseReceived += CommandManager_PauseReceived;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }
        else
        {
            await PauseAsync();
            mediaPlayer.Source = mediaPlaybackItem(media);
        }
    }
    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        PlayEnded.Invoke(sender, EventArgs.Empty);
        PlayNext.Invoke(sender, EventArgs.Empty);
    }
    private void CommandManager_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
    {
        PlayNext.Invoke(sender, EventArgs.Empty);
    }
    private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
    {
        PlayPrevious.Invoke(sender, EventArgs.Empty);
    }
    private void CommandManager_PauseReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPauseReceivedEventArgs args)
    {
        IsPlayingChanged.Invoke(sender, IsPlaying);
    }
}
