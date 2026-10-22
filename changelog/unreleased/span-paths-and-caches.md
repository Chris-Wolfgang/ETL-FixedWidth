type: internal

net5.0, net6.0 and net7.0 builds now take the span-based (allocation-free) numeric, date and padding paths that were gated on net8.0; the binary field map is built once per record type; empty value-type fields reuse one boxed default per type.
