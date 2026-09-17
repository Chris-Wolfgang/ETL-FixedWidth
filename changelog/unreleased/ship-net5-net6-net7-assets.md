type: fix

Ship `net5.0`, `net6.0` and `net7.0` assemblies: the `netstandard2.0` build, loaded beside the `net5.0`+ `Wolfgang.Etl.Abstractions` asset, would throw `MissingMethodException` on any write to an inherited options-record property (`IsExternalInit` modreq mismatch). Each runtime now gets an assembly compiled against its matching Abstractions asset.
