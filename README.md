# IdolyPrideExperiments
Experiments involving アイプラ assets.

## Extractor
Extracts the encrypted asset bundles from the asset directory.
For now my reimplementation of their asset manager (Octo) is not perfect but it works if the 
ProtoBuf database is deserialized manually.

## GachaSimulator
Using code from Extractor, reads specific audio assets from the specified `octo` directory to
simulate the gacha. If none is specified, it will fallback to reading the raw assets in the same
directory. Said files can be downloaded in the Assets folder of the project.

Some of the triggers are based on the audio, so using different assets will have unexpected results.