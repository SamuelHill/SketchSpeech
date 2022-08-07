using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace NU.Kiosk
{
    [ServiceContract]
    public interface IAudioStreamReceiver
    {
        [OperationContract(IsOneWay = true)]
        void AcceptAudio(SimpleAudioBuffer buffer);

        [OperationContract]
        bool IsAlive();
    }

    [DataContract]
    public struct SimpleAudioBuffer
    {
        [DataMember]
        public byte[] Data { get; private set; }
        [DataMember]
        public uint Length { get; private set; }
        [DataMember]
        public DateTime Timestamp { get; private set; }
        [DataMember]
        public bool EndOfCurrentCommunication { get; private set; }
        [DataMember]
        public bool Ready { get; private set; }

        public SimpleAudioBuffer(byte[] Data)
        {
            this.EndOfCurrentCommunication = false;
            this.Length = (uint)Data.Length;
            this.Data = Data;
            this.Timestamp = DateTime.MinValue;
            this.Ready = false;
        }

        public void Stamp(uint Length, DateTime OriginatingTimeStamp, bool EndOfCommunication)
        {
            Ready = true;
        }
    }
}
