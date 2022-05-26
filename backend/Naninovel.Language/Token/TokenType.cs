namespace Naninovel.Language;

public enum TokenType
{
    EmptyLine = 0,
    CommentLine = 1,
    CommentText = 2,
    LabelLine = 3,
    LabelText = 4,
    GenericTextLine = 5,
    CommandLine = 6,
    Command = 7,
    CommandIdentifier = 8,
    Parameter = 9,
    ParameterIdentifier = 10,
    ParameterValue = 11,
    InlinedCommand = 12,
    Expression = 13,
    GenericTextPrefix = 14,
    GenericTextAuthor = 15,
    GenericTextAuthorAppearance = 16,
    Error = 17
}
