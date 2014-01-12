csharp-object-to-object-literal
===============================

This is an attempt to emit a C# object literal constructor for a NHibernate object (graph) in memory, which can then be pasted into any unit test framework.

It is not a general case solution, but it does work on my real world data.  As I encounter more data types, I will enhance it.  If you find a feature you wish, feel free to contact me or add it yourself.

The basic approach is to walk all the "intrinsic" properties of the object (String, int, etc.) and emit the C# Object Literal Constructor syntax.  Then it walks all other properties, assigning them once it has recursed thru the class.  Whenever it encounters a List<T> or Array, it will create seperate var declarations for each, then it will emit the list once all the new variables have been defined.

This code has no dependancy on NHibernate, and should port easily to any other O/RM, as long as you can identify when two entities are equal.  In this case, all our entities inherit a Key propery from a base class, and that coupled with a Type identifies an entity.  Since my purpose is to serialize a just-loaded NHibernate object graph, all entities will have keys.




