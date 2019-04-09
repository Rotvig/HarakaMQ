using System.Reflection;
using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;
using HarakaMQ.UDPCommunication.Models;

namespace HarakaMQ.UnitTests.Utils
{
    public class AutoFakeItEasyDataAttribute : AutoDataAttribute
    {
        public AutoFakeItEasyDataAttribute() : base(() =>
            new Fixture()
                .Customize(new AutoFakeItEasyCustomization()))
        {
            
        }
    }
    
    public class AutoFakeItEasyDataAttributeWithExtendedPacketInformationSpecimenBuilder : AutoDataAttribute
    {
        public AutoFakeItEasyDataAttributeWithExtendedPacketInformationSpecimenBuilder() : base(() =>
        {
            var fixture =  new Fixture()
                .Customize(new AutoFakeItEasyCustomization());
            fixture.Customizations.Add(new ExtendedPacketInformationSpecimenBuilder());
            return fixture;
        })
        {
            
        }
    }
    
    public class ExtendedPacketInformationSpecimenBuilder : ISpecimenBuilder
    {
        private int _seqNo;
        public object Create(object request, ISpecimenContext context)
        {
            if (!(request is ParameterInfo pi))
            {
                return new NoSpecimen();
            }
            if (pi.ParameterType != typeof(ExtendedPacketInformation))
            {
                return new NoSpecimen();
            }
            _seqNo += 1;
            var packet = context.Create<Packet>();
            packet.ReturnPort = 80;
            packet.SeqNo = _seqNo;
            var extendedPacketInformation = context.Create<ExtendedPacketInformation>();
            extendedPacketInformation.Host = new Host(){IPAddress = "1234a", Port = 80};
            extendedPacketInformation.UdpMessageType = UdpMessageType.Packet;
            extendedPacketInformation.Packet = packet;

            return extendedPacketInformation;
        }
    }
}