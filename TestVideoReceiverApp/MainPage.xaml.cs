using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;

using Windows.UI.Core;
using Windows.Media.Playback;
using Windows.Media.Core;

using SympleRtcCore;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestVideoReceiverApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        StarWebrtcContext starWebrtcContext;
        
        //MediaPlayer _mediaPlayer;

        public MainPage()
        {
            this.InitializeComponent();

            Debug.WriteLine("MainPage()");

            //_mediaPlayer = new MediaPlayer();
            //mediaPlayerElement.SetMediaPlayer(_mediaPlayer);

            starWebrtcContext = StarWebrtcContext.CreateMentorContext();
            // right after creating the context (before starting the connections), we could edit some parameters such as the signalling server
            starWebrtcContext.ReceiveVideoMediaElement = mediaElement; // the MediaElement that is on the XAML layout

            // comment these out if not needed
            //Messenger.AddListener<string>(SympleLog.LogTrace, OnLog);
            Messenger.AddListener<string>(SympleLog.LogDebug, OnLog);
            Messenger.AddListener<string>(SympleLog.LogInfo, OnLog);
            Messenger.AddListener<string>(SympleLog.LogError, OnLog);

            //Messenger.AddListener<IMediaSource>(SympleLog.CreatedMediaSource, OnCreatedMediaSource);
            //Messenger.AddListener(SympleLog.DestroyedMediaSource, OnDestroyedMediaSource);


            Messenger.AddListener(SympleLog.RemoteAnnotationReceiverConnected, OnRemoteAnnotationReceiverConnected);
            Messenger.AddListener(SympleLog.RemoteAnnotationReceiverDisconnected, OnRemoteAnnotationReceiverDisconnected);

            Messenger.AddListener<Org.WebRtc.Media, Org.WebRtc.MediaVideoTrack>(SympleLog.RemoteStreamAdded, OnRemoteStreamAdded);
            Messenger.AddListener<Org.WebRtc.Media, Org.WebRtc.MediaVideoTrack>(SympleLog.RemoteStreamRemoved, OnRemoteStreamRemoved);

        }

        private void OnRemoteStreamRemoved(Org.WebRtc.Media media, Org.WebRtc.MediaVideoTrack peerVideoTrack)
        {
            Messenger.Broadcast(SympleLog.LogDebug, "OnRemoteStreamRemoved");
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                Messenger.Broadcast(SympleLog.LogInfo, "Removing video track media element pair");
                media.RemoveVideoTrackMediaElementPair(peerVideoTrack);

            }
            );
        }

        private void OnRemoteStreamAdded(Org.WebRtc.Media media, Org.WebRtc.MediaVideoTrack peerVideoTrack)
        {
            Messenger.Broadcast(SympleLog.LogDebug, "OnRemoteStreamAdded");

            

            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                Messenger.Broadcast(SympleLog.LogInfo, "Adding video track media element pair");
                media.AddVideoTrackMediaElementPair(peerVideoTrack, mediaElement, SymplePlayerEngineWebRTC.RemotePeerVideoTrackId);
                Messenger.Broadcast(SympleLog.LogInfo, "Done adding video track media element pair");

                //MediaSource createdSource = MediaSource.CreateFromIMediaSource(source);

                //_mediaPlayer.Source = createdSource;
                //_mediaPlayer.Play();

            }
            );
        }

        public void PeerVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Debug.WriteLine("PeerVideo_MediaFailed");
        }

        private void OnRemoteAnnotationReceiverConnected()
        {
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                sendTestMessageToAnnotationReceiverButton.IsEnabled = true;
            }
            );
        }

        private void sendTestMessageToAnnotationReceiverButton_Click(object sender, RoutedEventArgs e)
        {
            JObject testMessageObj = new JObject();
            testMessageObj["foo"] = "bar";
            testMessageObj["baz"] = "bat";

            starWebrtcContext.sendMessageToAnnotationReceiver(testMessageObj);
        }

        private void OnRemoteAnnotationReceiverDisconnected()
        {
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                sendTestMessageToAnnotationReceiverButton.IsEnabled = false;
            }
            );
        }

        private void OnLog(string msg)
        {
            Debug.WriteLine(msg);

            // http://stackoverflow.com/questions/19341591/the-application-called-an-interface-that-was-marshalled-for-a-different-thread
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                // Your UI update code goes here!
                textBox.Text += msg + "\n";
            }
            );

        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            button.IsEnabled = false;

            starWebrtcContext.initAndStartWebRTC();
        }


    }
}
