// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using SiliconStudio.Core;

namespace SiliconStudio.Xenko.Shaders
{
    /// <summary>
    /// A shader source that is linked to a xkfx effect.
    /// </summary>
    [DataContract("ShaderMixinGeneratorSource")]
    public sealed class ShaderMixinGeneratorSource : ShaderSource, IEquatable<ShaderMixinGeneratorSource>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderMixinGeneratorSource"/> class.
        /// </summary>
        public ShaderMixinGeneratorSource()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderMixinGeneratorSource"/> class.
        /// </summary>
        /// <param name="name">The name of the xkfx effect.</param>
        public ShaderMixinGeneratorSource(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the name of the xkfx effect.
        /// </summary>
        /// <value>The name of the xkfx effect.</value>
        public string Name { get; set; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(ShaderMixinGeneratorSource other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name);
        }

        public override object Clone()
        {
            return new ShaderMixinGeneratorSource(Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ShaderMixinGeneratorSource && Equals((ShaderMixinGeneratorSource)obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public static bool operator ==(ShaderMixinGeneratorSource left, ShaderMixinGeneratorSource right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ShaderMixinGeneratorSource left, ShaderMixinGeneratorSource right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return string.Format("xkfx {0}", Name);
        }
    }
}