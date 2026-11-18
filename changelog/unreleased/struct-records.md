type: fix

Struct (`record struct`) record types now work with the text and binary extractors: the factory and field setters operate on the boxed instance, so fields are no longer lost, and the misleading "no public parameterless constructor" error is gone.
