# MetaJson
`MetaJson` is a .Net 5 Source Generator geared toward Json (de)serialization.
The goal is to use compile-time knowledge of declared types to generate efficients, reflection-less implementations.

## DriverApp project
This project contains a simple console application that uses Roslyn to compile the `SampleApp` with the `MetaJson` source generator.

## MetaJson project
This project contains the source generator itself.

## SampleApp project
This project contains a sample usage. It serves mainly for two things.
1. Ensure that the final generated Json file is as expected.
2. Ensure that the consuming workflow (intellisense / build) is adequate.
