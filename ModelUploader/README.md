# The ADT Model Uploader sample

**Author:** Juergen Mayrbaeurl

This tool simplifies batch uploading of DTDL models, such as the [Digital Twins Definition Language (DTDL) ontology for Smart Cities](https://github.com/Azure/opendigitaltwins-smartcities), into an Azure Digital Twins instance.

The code borrows extensively from the [Tools for Open Digital Twins Definition Language (DTDL) based Ontologies](https://github.com/Azure/opendigitaltwins-tools)

## How it works

Modeluploader simply traverses the contents of a directory tree via a breadth-first search (using the .NET [Directory.EnumerateFiles](https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles?view=netcore-3.1) method) to select files for upload, and then uploads them all at once using the ADT SDK.


## Example usage

`./ModelUploader -p /Users/karl/DTDLModels/ -i <your_adt_instance_url>`

## Example usage with Deleting All Models First

The "-d" option recursively deletes ALL Models from ADT Instance. You cannot use the -d option alone in this version (a -p option must be specified)

`./ModelUploader -p /Users/karl/DTDLModels/ -d -i <your_adt_instance_url>`

## Example usage with Uploading more than one set of models

In order for this to work, the user must separate the models  into multiple different directories:

`./ModelUploader -p /Users/karl/DTDLModels/REC -i <your_adt_instance_url>`

`./ModelUploader -p /Users/karl/DTDLModels/Willow -i <your_adt_instance_url>` 

`./ModelUploader -p /Users/karl/DTDLModels/FoaF -i <your_adt_instance_url>` 

...etc
