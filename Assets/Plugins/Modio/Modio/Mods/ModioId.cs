using System;

namespace Modio.Mods
{
    public readonly struct ModioId : IEquatable<ModioId>
    {
        readonly long _id;

        public ModioId(long id) => _id = id;

        public bool IsValid() => _id > 0;

        internal long GetResourceId() => _id;

        public static bool operator ==(ModioId left, ModioId right) => left._id == right._id;
        public static bool operator !=(ModioId left, ModioId right) => left._id != right._id;

        public override bool Equals(object obj) => obj is ModioId other && this == other;
        public override int GetHashCode() => _id.GetHashCode();

        public static implicit operator long(ModioId modioId) => modioId._id;
        public static implicit operator ModioId(long id) => new ModioId(id);

        public override string ToString() => _id.ToString();

        public bool Equals(ModioId other) => _id == other._id;
    }
}
