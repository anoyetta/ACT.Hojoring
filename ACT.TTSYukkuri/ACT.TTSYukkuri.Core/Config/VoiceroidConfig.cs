using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using ACT.TTSYukkuri.Voiceroid;
using FFXIV.Framework.Common;
using Prism.Commands;
using Prism.Mvvm;
using RucheHome.Voiceroid;

namespace ACT.TTSYukkuri.Config
{
    [Serializable]
    public class VoiceroidConfig :
        BindableBase
    {
        public VoiceroidConfig()
        {
#if DEBUG
            if (WPFHelper.IsDesignMode)
            {
                var processes = (new ProcessFactory()).Processes;
                foreach (var inner in processes)
                {
                    this.Voiceroids.Add(new VoiceroidProcess()
                    {
                        InnerProcess = inner
                    });
                }
            }
#endif
        }

        private VoiceroidId selectedVoiceroidId = VoiceroidId.YukariEx;
        private ObservableCollection<VoiceroidProcess> voiceroids = new ObservableCollection<VoiceroidProcess>();
        private bool exitVoiceroidWhenExit = true;
        private bool directSpeak = true;

        public VoiceroidId SelectedVoiceroidId
        {
            get => this.selectedVoiceroidId;
            set => this.SetProperty(ref this.selectedVoiceroidId, value);
        }

        public ObservableCollection<VoiceroidProcess> Voiceroids => this.voiceroids;

        public bool ExitVoiceroidWhenExit
        {
            get => this.exitVoiceroidWhenExit;
            set => this.SetProperty(ref this.exitVoiceroidWhenExit, value);
        }

        public bool DirectSpeak
        {
            get => this.directSpeak;
            set => this.SetProperty(ref this.directSpeak, value);
        }

        public void Load()
        {
            var ctrl = SpeechController.Default as VoiceroidSpeechController;
            if (ctrl == null)
            {
                return;
            }

            foreach (var p in ctrl.ProcessFactory.Processes)
            {
                var voiceroid = this.Get(p.Id);
                if (voiceroid == null)
                {
                    voiceroid = new VoiceroidProcess();
                    this.Voiceroids.Add(voiceroid);
                }

                if (voiceroid.InnerProcess == null)
                {
                    voiceroid.InnerProcess = p;
                }
            }
        }

        public VoiceroidProcess Get(
            VoiceroidId id) =>
            this.Voiceroids.FirstOrDefault(x => x.Id == id);

        public VoiceroidProcess GetSelected() =>
            this.Get(this.SelectedVoiceroidId);

        public override string ToString() =>
            $"{nameof(this.SelectedVoiceroidId)}:{this.SelectedVoiceroidId}," +
            $"{nameof(this.DirectSpeak)}:{this.DirectSpeak}";
    }

    [Serializable]
    public class VoiceroidProcess :
        BindableBase
    {
        private IProcess innerProcess;
        private VoiceroidId id;
        private string name;
        private string path;

        [XmlIgnore]
        public IProcess InnerProcess
        {
            get => this.innerProcess;
            set
            {
                if (this.SetProperty(ref this.innerProcess, value))
                {
                    this.id = this.innerProcess.Id;
                    this.Name = this.innerProcess.Name;
                }
            }
        }

        public VoiceroidId Id
        {
            get => this.id;
            set => this.SetProperty(ref this.id, value);
        }

        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        public string Path
        {
            get => this.path;
            set
            {
                if (this.SetProperty(ref this.path, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsFound));
                }
            }
        }

        public bool IsFound =>
            !string.IsNullOrEmpty(this.Path) && File.Exists(this.Path);

        private ICommand powerCommand;

        public ICommand PowerCommand =>
            this.powerCommand ?? (this.powerCommand = new DelegateCommand<VoiceroidProcess>(async (x) =>
            {
                if (x == null)
                {
                    return;
                }

                if (x.InnerProcess.IsRunning)
                {
                    await x.InnerProcess.Exit();
                }
                else
                {
                    if (x.IsFound)
                    {
                        await x.InnerProcess.Run(x.Path);
                    }
                }
            }));
    }
}
