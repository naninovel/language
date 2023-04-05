namespace Naninovel.Language;

public enum TokenType
{
    Error = 0,
    EmptyLine = 1,
    CommentLine = 2,
    CommentText = 3,
    LabelLine = 4,
    LabelText = 5,
    GenericTextLine = 6,
    CommandLine = 7,
    Command = 8,
    CommandIdentifier = 9,
    Parameter = 10,
    ParameterIdentifier = 11,
    ParameterValue = 12,
    InlinedCommand = 13,
    Expression = 14,
    GenericTextPrefix = 15,
    GenericTextAuthor = 16,
    GenericTextAuthorAppearance = 17,
    TextIdentifier = 18
}
