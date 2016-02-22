# ARGus
Simple .NET Commandline Parser that generates a generic object instance from the given arguments

## Example:
``` csharp
static void Main(string[] args)
{
	bool success;
	var options = Argus.Parse<Options>(args, out success);

	if(!success)
		Argus.PrintUsage<Options>();
}

class Options
{
	[ImplicitArgument(0, name: "Config file", description: "The path to the config file")]
	public string ConfigFile { get; set; }

	[ExplicitArgument("Date", 'd', "Sets the execution date")]
	public DateTime Date { get; set; }

	[SwitchArgument("Force", 'f', "Forces the operation")]
	public bool Force { get; set; }

	[ImplicitArgument(1, true, "Target files", "A list of all target files")]
	public IEnumerable<string> TargetFiles { get; set; }
}
```


##The generated example output:
![Example generated output](http://i.imgur.com/fqDDBsd.png)


## Which Attribute do I need?

* __ImplicitArgument__ if it's a mandatory argument
* __ExplicitArgument__ if it's not mandatory and has a value
* __SwitchArgument__ if it's not mandatory and has no value (true if set, false if not)


## What data types can I use?

Many .NET date types like ```DateTime, int, float, ...``` are supported. The only exception is if an ImplicitArgument has "isParams" set to true. In this case the data type has to be ```IEnumerable<string>```
