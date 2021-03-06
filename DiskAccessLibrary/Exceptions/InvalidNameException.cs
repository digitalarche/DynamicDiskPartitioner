/* Copyright (C) 2019 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System.IO;

namespace DiskAccessLibrary
{
    public class InvalidNameException : IOException
    {
        public InvalidNameException() : this("The filename, directory name, or volume label syntax is incorrect")
        {
        }

        public InvalidNameException(string message) : base(message)
        {
#if Win32
            HResult = IOExceptionHelper.GetHResultFromWin32Error(Win32Error.ERROR_INVALID_NAME);
#endif
        }
    }
}
