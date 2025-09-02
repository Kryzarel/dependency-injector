# Dependency Injector

Dependency Injection framework for C# and Unity. This package contains pure C# code only. Even though it is formatted as Unity package, it *should* be usable in any C# project. An additional package will be released *soon(tm)* which contains Unity-specific code for better integration with the engine.

### YouTube Tutorial: https://youtu.be/Z0J-PDZBhqc

Made mainly for personal use and educational purposes. Watch the video for a step-by-step tutorial on how it was implemented.

## Installation

### Dependencies

[Dependency Injector](https://github.com/Kryzarel/dependency-injector) requires the [C# Utilities](https://github.com/Kryzarel/c-sharp-utilities) package to be installed.

### Install via Git URL

1. Navigate to your project's Packages folder and open the `manifest.json` file.
2. Add these two lines:
	-	```json
		"com.kryzarel.dependency-injector": "https://github.com/Kryzarel/dependency-injector.git",
		```
	-	```json
		"com.kryzarel.c-sharp-utilities": "https://github.com/Kryzarel/c-sharp-utilities.git",
		```

### Install manually

1. Clone or download the [Dependency Injector](https://github.com/Kryzarel/dependency-injector) and the [C# Utilities](https://github.com/Kryzarel/c-sharp-utilities) repositories.
2. Copy/paste or move both repository folders directly into your project's Packages folder or into the Assets folder.

## Usage Examples

### Basic Usage

```csharp
Builder builder = new Builder();

builder.Register<ISingleton, ConcreteSingleton>(Lifetime.Singleton); // Singleton registration (only 1 instance per registration will exist)
builder.Register<IScoped, ConcreteScoped>(Lifetime.Scoped); // Scoped registration (only 1 instance per container will exist)
builder.Register<ITransient, ConcreteTransient>(Lifetime.Transient); // Transient registration (a new instance will be created every time it is requested)

IContainer container = builder.Build(); // Build a container with the registrations. The created Container's registrations are read-only

// You can keep registering in the builder and building new containers from it. Those containers will be totally independent from each other

ISingleton singleton = container.ResolveObject<ISingleton>(); // Get the object registered to ISingleton. The underlying object is of type ConcreteSingleton
IScoped scoped = container.ResolveObject<IScoped>(); // Get the object registered to IScoped. The underlying object is of type ConcreteScoped
ITransient transient = container.ResolveObject<ITransient>(); // Get the object registered to ITransient. The underlying object is of type ConcreteTransient
```
When using `ResolveObject<T>()` an exception will be thrown if the registration is not found. Use `TryResolveObject<T>(out T obj)` for `true/false` return value instead.

### Scopes aka Child Containers

```csharp
IContainer child = container.CreateScope(builder => {
	// Add or overwrite registrations
	// builder.Register<BaseType, DerivedType>(lifetime);
});
```
Child containers "inherit" all registrations from their parents and can add additional registrations or overwrite existing ones. For Singleton registrations, a single instance is shared between the parent and all of its child scopes (unless overwritten). For Scoped registrations, each child container will have its own separate instance.

### Lifetimes

```csharp
// This type is registered as Singleton, it will only be instantiated once. Calling this multiple times will return the same object.
ISingleton singleton = container.ResolveObject<ISingleton>();

// This type is registered as Scoped, it will only be instantiated once PER CONTAINER. Child containers will have their own separate instance. Calling this multiple times on the same container will return the same object.
IScoped scoped = container.ResolveObject<IScoped>();

// This type is registered as Transient, a new object will be instantiated every call.
ITransient transient = container.ResolveObject<ITransient>();

// Compare parent and child Singleton objects
if (container.ResolveObject<ISingleton>() == child.ResolveObject<ISingleton>())
{
	// Returns true. Same instance.
}

// Compare parent and child Scoped objects
if (container.ResolveObject<IScoped>() == child.ResolveObject<IScoped>())
{
	// Returns false. Different instances.
}
```

## Author

[Kryzarel](https://www.youtube.com/@Kryzarel)

## License

MIT