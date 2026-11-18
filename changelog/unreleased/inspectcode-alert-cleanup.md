type: internal

Removed four InspectCode findings without behaviour change: BinaryFieldMap collects its attributed properties in a loop instead of a LINQ pipeline needing null-forgiving operators (S8969), and two test helpers were tidied (S9415, RedundantEnumerableCastCall).
