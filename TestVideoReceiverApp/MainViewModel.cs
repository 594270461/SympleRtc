using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestVideoReceiverApp.MVVM;

using Org.WebRtc;
using Windows.UI.Core;
using Windows.UI.Popups;
using SympleRtcCore;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Windows.UI.Xaml;
using TestVideoReceiverApp.Utilities;
using Windows.UI.Xaml.Controls;

namespace TestVideoReceiverApp
{
    public delegate void InitializedDelegate();
    internal class MainViewModel : DispatcherBindableBase
    {
        public event InitializedDelegate OnInitialized;


        public MediaElement RemoteVideo;

        public TextBox LogTextBox;

        StarWebrtcContext starWebrtcContext;


        /// <summary>
        /// Constructor for MainViewModel.
        /// </summary>
        /// <param name="uiDispatcher">Core event message dispatcher.</param>
        public MainViewModel(CoreDispatcher uiDispatcher)
            : base(uiDispatcher)
        {
            // Initialize all the action commands
            InitStartWebRtcCommand = new ActionCommand(InitStartWebRtcCommandExecute, InitStartWebRtcCommandCanExecute);
            SendTestMessageToAnnotationReceiverCommand = new ActionCommand(SendTestMessageToAnnotationReceiverCommandExecute, SendTestMessageToAnnotationReceiverCommandCanExecute);
            
            starWebrtcContext = StarWebrtcContext.CreateMentorContext();
            starWebrtcContext.CoreDispatcher = uiDispatcher;
            // right after creating the context (before starting the connections), we could edit some parameters such as the signalling server

            // comment these out if not needed
            Messenger.AddListener<string>(SympleLog.LogTrace, OnLog);
            Messenger.AddListener<string>(SympleLog.LogDebug, OnLog);
            Messenger.AddListener<string>(SympleLog.LogInfo, OnLog);
            Messenger.AddListener<string>(SympleLog.LogError, OnLog);

            //Messenger.AddListener<IMediaSource>(SympleLog.CreatedMediaSource, OnCreatedMediaSource);
            //Messenger.AddListener(SympleLog.DestroyedMediaSource, OnDestroyedMediaSource);


            Messenger.AddListener(SympleLog.RemoteAnnotationReceiverConnected, OnRemoteAnnotationReceiverConnected);
            Messenger.AddListener(SympleLog.RemoteAnnotationReceiverDisconnected, OnRemoteAnnotationReceiverDisconnected);

            Messenger.AddListener<Org.WebRtc.Media, Org.WebRtc.MediaVideoTrack>(SympleLog.RemoteStreamAdded, OnRemoteStreamAdded);
            Messenger.AddListener<Org.WebRtc.Media, Org.WebRtc.MediaVideoTrack>(SympleLog.RemoteStreamRemoved, OnRemoteStreamRemoved);


            // Display a permission dialog to request access to the microphone and camera
            WebRTC.RequestAccessForMediaCapture().AsTask().ContinueWith(antecedent =>
            {
                if (antecedent.Result)
                {
                    Initialize(uiDispatcher);
                }
                else
                {
                    RunOnUiThread(async () =>
                    {
                        var msgDialog = new MessageDialog(
                            "Failed to obtain access to multimedia devices!");
                        await msgDialog.ShowAsync();
                    });
                }
            });

        }

        /// <summary>
        /// Application suspending event handler.
        /// </summary>
        public async Task OnAppSuspending()
        {
            // TODO: cancel connecting to peer

            // TODO: if connected to peer, disconnect from peer

            // TODO: if connected, disconnect from server
            
            Media.OnAppSuspending();
        }





        private void OnLog(string msg)
        {
            Debug.WriteLine(msg);

            RunOnUiThread(async () =>
            {
                // Your UI update code goes here!
                LogTextBox.Text += msg + "\n";
            });
        }



        /// <summary>
        /// Media Failed event handler for remote/peer video.
        /// Invoked when an error occurs in peer media source.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the exception routed event.</param>
        public void RemoteVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Debug.WriteLine("RemoteVideo_MediaFailed");

            // TODO: re-establish remote video
        }





        private void OnRemoteStreamRemoved(Org.WebRtc.Media media, Org.WebRtc.MediaVideoTrack peerVideoTrack)
        {
            Messenger.Broadcast(SympleLog.LogDebug, "OnRemoteStreamRemoved");

            RunOnUiThread(async () =>
            {
                Messenger.Broadcast(SympleLog.LogInfo, "Removing video track media element pair");
                media.RemoveVideoTrackMediaElementPair(peerVideoTrack);
            });
        }

        private void OnRemoteStreamAdded(Org.WebRtc.Media media, Org.WebRtc.MediaVideoTrack peerVideoTrack)
        {
            Messenger.Broadcast(SympleLog.LogDebug, "OnRemoteStreamAdded");

            RunOnUiThread(async () =>
            {
                Messenger.Broadcast(SympleLog.LogInfo, "Adding video track media element pair");
                media.AddVideoTrackMediaElementPair(peerVideoTrack, RemoteVideo, SymplePlayerEngineWebRTC.RemotePeerVideoTrackId);
                Messenger.Broadcast(SympleLog.LogInfo, "Done adding video track media element pair");

                //MediaSource createdSource = MediaSource.CreateFromIMediaSource(source);

                //_mediaPlayer.Source = createdSource;
                //_mediaPlayer.Play();

            });
        }

        private void OnRemoteAnnotationReceiverConnected()
        {
            IsAnnotationReceiverConnected = true;
        }
        
        private void OnRemoteAnnotationReceiverDisconnected()
        {
            IsAnnotationReceiverConnected = false;
        }
        

        /// <summary>
        /// The initializer for MainViewModel.
        /// </summary>
        /// <param name="uiDispatcher">The UI dispatcher.</param>
        public void Initialize(CoreDispatcher uiDispatcher)
        {
            RunOnUiThread(() =>
            {
                OnInitialized?.Invoke();
            });
        }














        private void SendTestMessageToAnnotationReceiverCommandExecute(object obj)
        {
            new Task(() =>
            {
                JObject testMessageObj = new JObject();
                testMessageObj["foo"] = "bar";
                testMessageObj["baz"] = "bat";

                starWebrtcContext.sendMessageToAnnotationReceiver(testMessageObj);
            }).Start();
        }





        private void InitStartWebRtcCommandExecute(object obj)
        {
            new Task(() =>
            {
                IsConnecting = true;

                starWebrtcContext.initAndStartWebRTC();
            }).Start();
        }





        private ActionCommand _initStartWebRtcCommand;
        
        public ActionCommand InitStartWebRtcCommand
        {
            get { return _initStartWebRtcCommand; }
            set { SetProperty(ref _initStartWebRtcCommand, value); }
        }

        private bool InitStartWebRtcCommandCanExecute(object obj)
        {
            return !IsConnected && !IsConnecting;
        }



        private ActionCommand _sendTestMessageToAnnotationReceiverCommand;

        public ActionCommand SendTestMessageToAnnotationReceiverCommand
        {
            get { return _sendTestMessageToAnnotationReceiverCommand; }
            set { SetProperty(ref _sendTestMessageToAnnotationReceiverCommand, value); }
        }

        private bool SendTestMessageToAnnotationReceiverCommandCanExecute(object obj)
        {
            return IsAnnotationReceiverConnected;
        }









        private bool _isAnnotationReceiverConnected;

        public bool IsAnnotationReceiverConnected
        {
            get { return _isAnnotationReceiverConnected; }
            set
            {
                SetProperty(ref _isAnnotationReceiverConnected, value);
                SendTestMessageToAnnotationReceiverCommand.RaiseCanExecuteChanged();
            }
        }













        private bool _isConnected;

        /// <summary>
        /// Indicator if the user is connected to the server.
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                SetProperty(ref _isConnected, value);
                InitStartWebRtcCommand.RaiseCanExecuteChanged();
                //DisconnectFromServerCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isConnecting;

        /// <summary>
        /// Indicator if the application is in the process of connecting to the server.
        /// </summary>
        public bool IsConnecting
        {
            get { return _isConnecting; }
            set
            {
                SetProperty(ref _isConnecting, value);
                InitStartWebRtcCommand.RaiseCanExecuteChanged();
            }
        }

    }
}
