using System.Text.Json.Serialization;

namespace Recrd.Core.Ast;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ActionStep), typeDiscriminator: "action")]
[JsonDerivedType(typeof(AssertionStep), typeDiscriminator: "assertion")]
[JsonDerivedType(typeof(GroupStep), typeDiscriminator: "group")]
public interface IStep { }
