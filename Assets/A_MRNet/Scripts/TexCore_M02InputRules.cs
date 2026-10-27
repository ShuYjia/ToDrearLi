using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Fixed M02 input rule sets. Every mode validates one authored install page:
/// <see cref="PasswordPair"/> and <see cref="RootPasswordPair"/> expect
/// [password, confirmation], <see cref="UserIdentityPair"/> expects
/// [userName, fullName].
/// </summary>
public enum M02InputValidationMode
{
    PasswordPair,
    UserIdentityPair,
    RootPasswordPair
}

/// <summary>
/// Stateless M02 input rules used by the authored password, user name and full
/// name pages. This type deliberately avoids UnityEngine types so the EditMode
/// tests and the local rule harness can exercise it directly.
/// </summary>
public static class TexCore_M02InputRules
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 64;
    public const int UserNameMinLength = 3;
    public const int UserNameMaxLength = 32;
    public const int FullNameMinLength = 2;
    public const int FullNameMaxLength = 40;

    /// <summary>
    /// Lowercase ASCII start, then lowercase letters, digits, '_' and '-'.
    /// </summary>
    public const string UserNamePattern = "^[a-z][a-z0-9_-]{2,31}$";

    public const string PasswordEmptyError = "密码不能为空";
    public const string PasswordLengthError = "密码长度必须为 8-64 位";
    public const string PasswordComplexityError = "密码必须包含大写字母、小写字母、数字和特殊字符";
    public const string PasswordWhitespaceError = "密码不能包含空格";
    public const string PasswordMismatchError = "两次输入的密码不一致";
    public const string UserNameLengthError = "用户名长度必须为 3-32 位";
    public const string UserNameStartError = "用户名必须以小写英文字母开头";
    public const string UserNameCharacterError = "用户名只能包含小写字母、数字、下划线和连字符";
    public const string FullNameLengthError = "全名长度必须为 2-40 个字符";
    public const string FullNameLetterError = "全名至少需要包含两个字母或汉字";
    public const string FullNameCharacterError = "全名不能包含数字或非法符号";
    public const string FullNameEdgeSpaceError = "全名不能以空格开头或结尾";
    public const string FullNameDoubleSpaceError = "全名不能包含连续空格";

    private static readonly Regex UserNameRegex = new Regex(
        UserNamePattern, RegexOptions.CultureInvariant);

    public static bool TryValidate(
        M02InputValidationMode mode,
        IReadOnlyList<string> values,
        out string error)
    {
        switch (mode)
        {
            case M02InputValidationMode.PasswordPair:
            case M02InputValidationMode.RootPasswordPair:
                return TryValidatePasswordPair(values, out error);
            case M02InputValidationMode.UserIdentityPair:
                return TryValidateUserIdentityPair(values, out error);
            default:
                error = string.Empty;
                return true;
        }
    }

    public static bool TryValidatePasswordPair(IReadOnlyList<string> values, out string error)
    {
        string password = ValueAt(values, 0);
        string confirmation = ValueAt(values, 1);
        if (!TryValidatePassword(password, out error))
        {
            return false;
        }

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            error = PasswordMismatchError;
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidateUserIdentityPair(IReadOnlyList<string> values, out string error)
    {
        if (!TryValidateUserName(ValueAt(values, 0), out error))
        {
            return false;
        }

        return TryValidateFullName(ValueAt(values, 1), out error);
    }

    /// <summary>
    /// 8-64 printable ASCII characters built from the four required classes. A
    /// character outside those classes, for example a non-ASCII letter, reports
    /// the same complexity error because the password is then not made of the
    /// required classes.
    /// </summary>
    public static bool TryValidatePassword(string value, out string error)
    {
        string password = value ?? string.Empty;
        if (password.Length == 0)
        {
            error = PasswordEmptyError;
            return false;
        }

        if (password.Length < PasswordMinLength || password.Length > PasswordMaxLength)
        {
            error = PasswordLengthError;
            return false;
        }

        bool hasUpper = false;
        bool hasLower = false;
        bool hasDigit = false;
        bool hasSpecial = false;
        foreach (char character in password)
        {
            if (character >= 'A' && character <= 'Z') hasUpper = true;
            else if (character >= 'a' && character <= 'z') hasLower = true;
            else if (character >= '0' && character <= '9') hasDigit = true;
            else if (IsPasswordSpecial(character)) hasSpecial = true;
            else if (char.IsWhiteSpace(character))
            {
                error = PasswordWhitespaceError;
                return false;
            }
            else
            {
                error = PasswordComplexityError;
                return false;
            }
        }

        if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
        {
            error = PasswordComplexityError;
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidateUserName(string value, out string error)
    {
        string userName = value ?? string.Empty;
        if (userName.Length < UserNameMinLength || userName.Length > UserNameMaxLength)
        {
            error = UserNameLengthError;
            return false;
        }

        if (userName[0] < 'a' || userName[0] > 'z')
        {
            error = UserNameStartError;
            return false;
        }

        if (!UserNameRegex.IsMatch(userName))
        {
            error = UserNameCharacterError;
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// 2-40 characters with at least two Unicode letters. Chinese characters,
    /// ASCII letters, single spaces, '-' and '\'' are allowed.
    /// </summary>
    public static bool TryValidateFullName(string value, out string error)
    {
        string fullName = value ?? string.Empty;
        if (fullName.Length < FullNameMinLength || fullName.Length > FullNameMaxLength)
        {
            error = FullNameLengthError;
            return false;
        }

        if (fullName[0] == ' ' || fullName[fullName.Length - 1] == ' ')
        {
            error = FullNameEdgeSpaceError;
            return false;
        }

        if (fullName.Contains("  "))
        {
            error = FullNameDoubleSpaceError;
            return false;
        }

        int letterCount = 0;
        foreach (char character in fullName)
        {
            if (char.IsLetter(character))
            {
                letterCount++;
                continue;
            }

            if (character == ' ' || character == '-' || character == '\'')
            {
                continue;
            }

            error = FullNameCharacterError;
            return false;
        }

        if (letterCount < 2)
        {
            error = FullNameLetterError;
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsPasswordSpecial(char character)
    {
        return character > 32 && character < 127 &&
               !(character >= 'A' && character <= 'Z') &&
               !(character >= 'a' && character <= 'z') &&
               !(character >= '0' && character <= '9');
    }

    private static string ValueAt(IReadOnlyList<string> values, int index)
    {
        if (values == null || index < 0 || index >= values.Count)
        {
            return string.Empty;
        }

        return values[index] ?? string.Empty;
    }
}
