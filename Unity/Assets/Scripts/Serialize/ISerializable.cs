// using Lockstep.Core.Logic.Serialization.Utils;

using Lockstep.Serialization;

namespace Lockstep.Core.Logic.Serialization
{

    public interface ISerializable
    {
        void Serialize(Serializer writer);

        void Deserialize(Deserializer reader);
    }
}