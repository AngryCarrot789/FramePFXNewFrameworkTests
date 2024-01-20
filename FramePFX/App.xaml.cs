using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FramePFX.Editors;
using FramePFX.Editors.Timelines;
using FramePFX.Editors.Timelines.Tracks;
using FramePFX.Editors.Timelines.Tracks.Clips;
using FramePFX.Editors.Views;

namespace FramePFX {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private void App_OnStartup(object sender, StartupEventArgs e) {
            VideoEditor editor = new VideoEditor();
            editor.LoadDefaultProject();

            EditorWindow window = new EditorWindow();
            window.Show();

            this.Dispatcher.InvokeAsync(() => {
                window.Editor = editor;
                Timeline timeline = editor.CurrentProject.MainTimeline;

                timeline.Tracks[0].DisplayName = "sex!";

                Task.Run(async () => {
                    for (int i = timeline.Tracks.Count - 1; i >= 0; i--) {
                        Track track = timeline.Tracks[i];
                        for (int j = track.Clips.Count - 1; j >= 0; j--) {
                            this.Dispatcher.Invoke(() => track.RemoveClipAt(j));
                            await Task.Delay(100);
                        }

                        this.Dispatcher.Invoke(() => timeline.RemoveTrackAt(i));
                        await Task.Delay(100);
                    }

                    this.Dispatcher.Invoke(() => timeline.PlayHeadPosition = 250);
                    await Task.Delay(500);

                    for (int i = 0; i < 6; i++) {
                        VideoTrack track = new VideoTrack();
                        this.Dispatcher.Invoke(() => timeline.AddTrack(track));
                        await Task.Delay(50);
                        for (int j = 0; j < 40; j++) {
                            this.Dispatcher.Invoke(() => track.AddClip(new VideoClip() {Span = new FrameSpan(j * 20L, 16), DisplayName = j.ToString()}));
                            await Task.Delay(50);
                        }
                    }
                });
            });
        }
    }
}
