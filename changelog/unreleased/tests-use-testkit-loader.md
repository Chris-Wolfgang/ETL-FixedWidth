type: internal

Tests use the TestKit TestLoader<T> in place of a hand-written counting loader and SynchronousProgress in place of a private progress double; the test that only exercised the removed double is gone. No library change.
