type: fix

`FixedWidthExtractor` and `FixedWidthMultiRecordExtractor` no longer read (and discard) one extra line when `MaximumItemCount` is reached, so `CurrentByteOffset` is an exact checkpoint after a capped run and no record is lost on resume.
