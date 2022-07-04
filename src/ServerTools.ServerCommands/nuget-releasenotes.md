# Changelog
All notable changes to this project will be documented in this file.

## [1.0.1] - not yet
### Added
- Added specific package ServerTools.ServerCommands.AzureStorageQueues, that is the implementation of generic for the Azure Storage service.
- Added specific package ServerTools.ServerCommands.AzureServiceBus, that is the implementation of generic for the Azure Service Bus service.
- Added generic package ServerTools.ServerCommands that can be used to create generic implementations of the functionality.
- Created ICloudCommands interface that need to be implemented to create new implementations of this functionality.
- To instantiate a new cloud commands object you need to call InitializeAsync() and pass it a command object and an implementaton of ConnectionOptions.
- Converted the dynamics Metadate implementation to a strong-typed CommandMetadata object.
- Added variuos extensions to CommandMetadata to easily populate the metadata details at specific times in the lifecycle
- Added a strong-types Message record that encapsulates the message details


## [1.0.1]
### Added
- Overloads for PostCommand() and PostResponse(), accepting Type and Type name
### Fixed 
- Issue with batch posting caused by inproper setting of type name



## [1.0.0] - 2021/12/01
### Added
- Validation of parameters when creating Commands
- Clear() method on Commands that clears out commands and responses, without executing them, as well all the corresponding queues  



> The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).