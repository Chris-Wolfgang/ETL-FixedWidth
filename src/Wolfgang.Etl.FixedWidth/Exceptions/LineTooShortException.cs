using System;

namespace Wolfgang.Etl.FixedWidth.Exceptions
{
    /// <summary>
    /// The exception thrown when a line in a fixed-width file is shorter than the total
    /// width required to read all defined fields. Derives from
    /// <see cref="MalformedLineException"/>.
    /// </summary>
    public sealed class LineTooShortException : MalformedLineException
    {
        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of <see cref="LineTooShortException"/> with full context.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        /// <param name="lineNumber">The 1-based line number of the offending line.</param>
        /// <param name="lineContent">The raw content of the offending line.</param>
        /// <param name="expectedWidth">The minimum number of characters required to read all fields.</param>
        /// <param name="actualWidth">The actual number of characters in the line.</param>
        public LineTooShortException
        (
            string message,
            long lineNumber,
            string lineContent,
            int expectedWidth,
            int actualWidth
        )
            : base
            (
                message,
                lineNumber,
                lineContent
            )
        {
            ExpectedWidth = expectedWidth;
            ActualWidth = actualWidth;
        }



        // ------------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------------

        /// <summary>
        /// The minimum number of characters required to read all defined fields, including
        /// any delimiter contribution.
        /// </summary>
        public int ExpectedWidth { get; }



        /// <summary>
        /// The actual number of characters in the offending line.
        /// </summary>
        public int ActualWidth { get; }
    }
}
