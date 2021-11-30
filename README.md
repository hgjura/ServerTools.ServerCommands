<h1 align="center">
  <a href="https://github.com/hgjura/ServerTools.ServerCommands">
    <!-- Please provide path to your logo here -->
    <img src=".github/logo.png" alt="Logo" width="650" height="150">
  </a>
</h1>

<div align="center">
  <h1>ServerTools.ServerCommands</h4>
  <br />
  <!-- <a href="#about"><strong>Explore the screenshots »</strong></a>
  <br />
  <br /> -->
  <a href="https://github.com/hgjura/ServerTools.ServerCommands/issues/new?assignees=&labels=bug&template=CONTRIBUTING.md&title=bug%3A+">Report a Bug</a>
  ·
  <a href="https://github.com/hgjura/ServerTools.ServerCommands/issues/new?assignees=&labels=enhancement&template=02_FEATURE_REQUEST.md&title=feat%3A+">Request a Feature</a>
  .
  <a href="https://github.com/hgjura/ServerTools.ServerCommands/issues/new?assignees=&labels=question&template=04_SUPPORT_QUESTION.md&title=support%3A+">Ask a Question</a>
</div>

<div align="center">
<br />

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![NuGet Badge](https://buildstats.info/nuget/ServerTools.ServerCommands)](https://www.nuget.org/packages/ServerTools.ServerCommands/) [![PRs welcome](https://img.shields.io/badge/PRs-welcome-ff69b4.svg?style=flat-square)](https://github.com/hgjura/ServerTools.ServerCommands/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22) [![Gitter](https://badges.gitter.im/hgjura/ServerCommands.svg)](https://gitter.im/hgjura/ServerCommands?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

[![code with hearth by Herald Gjura](https://img.shields.io/badge/%3C%2F%3E%20with%20%E2%99%A5%20by-hgjura-ff1414.svg?style=flat-square)](https://github.com/hgjura)

</div>

<details open="open">
<summary>Table of Contents</summary>

- [About](#about)
  - [Built With](#built-with)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
- [Usage](#usage)
- [Roadmap](#roadmap)
- [Support](#support)
- [Project assistance](#project-assistance)
- [Contributing](#contributing)
- [Authors & contributors](#authors--contributors)
- [Security](#security)
- [License](#license)
<!-- - [Acknowledgements](#acknowledgements) -->

</details>

---

## About

ServerCommands facilitates running of units of code or commands remotely. It incorporates principles of messaging architectures used by most messaging tools and frameworks, like [Azure Service Bus](https://docs.microsoft.com/en-ca/azure/service-bus-messaging/), [AWS SQS](https://aws.amazon.com/sqs/), [RabbitMQ](https://www.rabbitmq.com/), or [Azure Storage Queues](https://docs.microsoft.com/en-ca/azure/storage/queues/storage-dotnet-how-to-use-queues?tabs=dotnet), [Apache Kafka](https://kafka.apache.org/) without any of the knowledge and configuration expertise to manage such installations and configurations. 

Currently, the library requires an Azure Storage account to run. But by no means this is a major dependency. All messaging services work very similarly, and the choice to use Azure Storage is purely for simplictiy and cost. Azure Storage provides both storage and queueing service at a minimal cost. In future iterations, and upon demand, versions of this library that work with all the other messaging services will be provided.  

### Built With
- C# (NET 5.0)
- Azure Sdk (Azure Storage Queues)
- DryIoc
- Polly

## Getting Started

### Prerequisites
* Basic understanding of the Messaging Arhcitectures
* Understanding of the Command pattern in software development

### Installation
[![NuGet Badge](https://buildstats.info/nuget/ServerTools.ServerCommands)](https://www.nuget.org/packages/ServerTools.ServerCommands/)

Add the [NuGet package](https://www.nuget.org/packages/ServerTools.ServerCommands/) to all the projects you want to use it in.

* In Visual Studio - Tools > NuGet Package Manager > Manage Packages for Solution
* Select the Browse tab, search for ServerCommands
* Select ServerTools.ServerCommands
* Install into each project within your solution


## Usage

To post a remote command you first will need to create a command (in your own code) that inherits from ```IRemoteCommand```.  The interface ```IRemoteCommand``` will ask you to implement two elements:
* ```RequiresResponse```: Is a property that returns a ```bool``` that indicates if this command will retunr a response or not. More about this in the extended documentation here. For now just return ```false```.
* ```ExecuteAsync```: Is the method that gets executed remotely. This methid accepts two parameters of type ```dynamic```:
	* ```command``` contains the context of the command, or the command parameters. Note that cannot be a mismatch between what the command expects to run and the properties of the ```command``` parameter. For example, this sample command (```AddNumbersCommand```) expects that properties ```Number1``` and ```Number2``` two to be present in the parametr command and be of type ```int```. If they are not, this command will fail the execution.
	* ```meta``` contains metadata about the command. This is filled it by the library and mostly contains various ```DateTime.UtcNow``` timestamps as the command travels through the systems and goes through the stages. This is avaiable to you, but you don't have to do anything with it, unless you will deleve into more expert use-cases of correlation and ordering.

This method returns a ```tuple``` of object of type 
```(bool, Exception, dynamic, dynamic)``` containing four items.
* ```Item1```: returns ```true/false``` depending if the command executed succesfully or not.
* ```Item2```: in case ```Item1``` is false, this returns the Exception object, otherwise ```null```.
* ```Item3```: returns a ```dynamic``` object that contains the command context of the response. This **must** be populated (be not ```null``` if property ```RequiresResponse``` is set to ```true```). Otherwise return null.
* ```Item4```: returns a ```dynamic``` object that contains the metadata. This is when you may want to add additional metadata datapoints before returnig it to the caller. More about it in teh extended documentatino. This also **must** be populated (be not ```null``` if propertry ```RequiresResponse``` is set to ```true```). Otherwise return null.
' ```true/false``` depending if the command executed succesfully or not.

Also, **all** exceptions must be handled within the body of the ```ExecuteAsync```. Remember, these commads are executed remotely and asynchronously. There will be nothing ruturnd to you! An unhandled exception will simply put the command back in the queue, and it will be tried **five (5)** times and then it will be placed in a dead letter queue, where it will sit until you bring it back form there to handle it properly.

See example: 

```csharp
public class AddNumbersCommand : IRemoteCommand
{
    public bool RequiresResponse => false;

    public async Task<(bool, Exception, dynamic, dynamic)> ExecuteAsync(dynamic command, dynamic meta)
    {
		// must handle exceptions
        try
        {
            int n1 = (int)command.Number1;
            int n2 = (int)command.Number2;

            int result = n1 + n2;
			
			// set the first item to true indicating success, set the rest to null
            return await Task.FromResult<(bool, Exception, dynamic, dynamic)>((true, null, null, null));


        }
        catch (Exception ex)
        {
			// set the first item to false indicating failure, set second items to the Exception thrown, set the rest to null
            return await Task.FromResult<(bool, Exception, dynamic, dynamic)>((false, ex, null, meta));
        }
    }
}

```


Now that you have the command ready to be executed, you will need to post it to be executed remotely.

Ini its simplest form, to post a command to the server it is simply four lines of code (four steps).
* step 1: create an instance of the ```CommandContainer```. This is the IoC container that holds all registrations for the remote commands.
* step 2: register the command you created above with the IoC container. In its simplest form, command have a parameterless constructor (like the sample above). But this is unrealistic as we want our commands to do rich and complex things, so go here to see how you can created commands with parameters and how to register them.
* step 3: instantiate a ```Commands``` store object. This is the command center for your remote commands. It only had a handful of methods though, as the complexity is hidden internally. It requires a few parameters:
	* first parameter, is the command container we just created
	* second paremeter, is the Azure Storage account name, that the library will use in the back end. Note this is not the storage connaction string, it is only the account name, for example ```storage123```.      
	* third parameter, is the Azure Storage account key. Note this is not the storage connaction string, it is only one of the two keys provided to you in the Azure portal.
    

```csharp
var _container = new CommandContainer();

_container.RegisterCommand<AddNumbersCommand>();

var c = new Commands(_container, Environment.GetEnvironmentVariable("StorageAccounName"), Environment.GetEnvironmentVariable("StorageAccountKey"));

_ = await c.PostCommand<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });
```

Once you post the command, you will need to create an executing context to execute them. Usually this would run in an Azuer Function, an AKS container, a Windows service, a commandline, or other, and would run in a loop or in a schedule.

To execute the commands queued in a server you will need to create an executing context (a ```Commands``` object) and register **all** the commands that are expected to have been registered remotely. Failure to register the commands that are queed would meen that the executing context will receive a command that it cannot recognize and process, and as such it will send it eventually to the dead letter queue.

Since in our sample there is only one command, we are doing the registration in-line. In your code, as you add more and more commands, you may want to mantain a utility fucntion of class that register commands as you add them, and all their dependencies, and returns a fully registered command conatinerer object to be pased to the ```Commands```.

The ```ExecuteCommands`` takes no parameters, and returns a ```tuple``` of object of type 
```(bool, Exception, dynamic, dynamic)``` containing three items.

* ```Item1```: returns ```true/false``` depending if the command executed **all** the command in the queue succesfully or not.
* ```Item2```: contains the number of commands executed.
* ```Item3```: returns a ```dynamic``` object that contains the command context of the response. This **must** be populated (be not ```null``` if property ```RequiresResponse``` is set to ```true```). Otherwise return null.
* ```Item4```: returns a ```dynamic``` object that contains the metadata. This is when you may want to add additional metadata datapoints before returnig it to the caller. More about it in teh extended documentatino. This also **must** be populated (be not ```null``` if propertry ```RequiresResponse``` is set to ```true```). Otherwise return null.
' ```true/false``` depending if the command executed succesfully or not.


```csharp
var _container = new CommandContainer();

_container.RegisterCommand<AddNumbersCommand>();

var c = new Commands(_container, Environment.GetEnvironmentVariable("StorageAccounName"), Environment.GetEnvironmentVariable("StorageAccountKey"));

var result = await c.ExecuteCommands();

//check if something was wrong or if any items were processed at all
Assert.IsTrue(!result.Item1);

//check if 1 or more items were processed
Assert.IsTrue(result.Item2 > 0);

//check if there was any errors
Assert.IsTrue(result.Item3.Count > 0); //This value keeps the list of error messages that were encountered. After retrying 5 times the command is moved to the deadletterqueue.

```

And that's that!

For more detailed documentation and more complex use cases head to the offical documentation at [the GitHub repo](https://github.com/hgjura/ServerTools.ServerCommands). If theer are [questions](https://github.com/hgjura/ServerTools.ServerCommands/issues/new?assignees=hgjura&labels=question&title=ask%3A+) or [request new feautures](https://github.com/hgjura/ServerTools.ServerCommands/issues/new?assignees=hgjura&labels=request&title=newfeature%3A+) do not hesitate to post them there.


## Roadmap

See the [open issues](https://github.com/hgjura/ServerTools.ServerCommands/issues) for a list of proposed features (and known issues).

- [Top Feature Requests](https://github.com/hgjura/ServerTools.ServerCommands/issues?q=label%3Aenhancement+is%3Aopen+sort%3Areactions-%2B1-desc) (Add your votes using the 👍 reaction)
- [Top Bugs](https://github.com/hgjura/ServerTools.ServerCommands/issues?q=is%3Aissue+is%3Aopen+label%3Abug+sort%3Areactions-%2B1-desc) (Add your votes using the 👍 reaction)
- [Newest Bugs](https://github.com/hgjura/ServerTools.ServerCommands/issues?q=is%3Aopen+is%3Aissue+label%3Abug)

### Top ten upcoming features  
- [ ] Adopt the library to wrok with other backend services
  - [ ] Azure Service Bus
  - [ ] AWS SQS
  - [ ] Apache Kafka
  - [ ] Rabbit MQ
- [x] Enable batching, batch processing and command correlations
- [ ] Enable ordering and ordered processing through sessions


## Support

<!-- > **[?]**
> Provide additional ways to contact the project maintainer/maintainers. -->

Reach out to the maintainer at one of the following places:

- [GitHub issues](https://github.com/hgjura/ServerTools.ServerCommands/issues/new?assignees=&labels=question&template=04_SUPPORT_QUESTION.md&title=support%3A+)
- The email which is located [in GitHub profile](https://github.com/hgjura)

## Project assistance

If you want to say **thank you** or/and support active development of ServerTools.ServerCommands:

- Add a [GitHub Star](https://github.com/hgjura/ServerTools.ServerCommands) to the project.
- Tweet about the ServerTools.ServerCommands on your Twitter.
- Write interesting articles about the project on [Dev.to](https://dev.to/), [Medium](https://medium.com/) or personal blog.

Together, we can make ServerTools.ServerCommands **better**!

## Contributing

First off, thanks for taking the time to contribute! Contributions are what make the open-source community such an amazing place to learn, inspire, and create. Any contributions you make will benefit everybody else and are **greatly appreciated**.

We have set up a separate document containing our [contribution guidelines](.github/CONTRIBUTING.md).

Thank you for being involved!

## Authors & contributors

The original setup of this repository is by [Herald Gjura](https://github.com/hgjura).

For a full list of all authors and contributors, check [the contributor's page](https://github.com/hgjura/ServerTools.ServerCommands/contributors).

## Security

ServerTools.ServerCommands follows good practices of security, but 100% security can't be granted in software.
ServerTools.ServerCommands is provided **"as is"** without any **warranty**. Use at your own risk.

_For more info, please refer to the [security](.github/SECURITY.md)._

## License

This project is licensed under the **MIT license**.

Copyright 2021 [Herald Gjura](https://github.com/hgjura)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

<!-- ## Acknowledgements

> **[?]**
> If your work was funded by any organization or institution, acknowledge their support here.
> In addition, if your work relies on other software libraries, or was inspired by looking at other work, it is appropriate to acknowledge this intellectual debt too. -->
