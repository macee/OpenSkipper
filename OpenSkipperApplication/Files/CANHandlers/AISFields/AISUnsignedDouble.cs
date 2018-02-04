﻿/*
	Copyright (C) 2009-2010, Andrew Mason <amas008@users.sourceforge.net>
	Copyright (C) 2009-2010, Jason Drake <jdra@users.sourceforge.net>

	This file is part of Open Skipper.
	
	Open Skipper is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Open Skipper is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;

namespace CANHandler
{
    public class AISUnsignedDouble : AISField
    {
        public double Scale { get; set; }
        public double Offset { get; set; }

        public override double GetValue(AISData aisData, out FieldValueState valueState)
        {
            if (BitOffset + BitLength > 8 * aisData.AISBytes.Length)
            {
                valueState = FieldValueState.NotAvailable;
                return 0.0;
            }
            else
            {
                valueState = FieldValueState.Valid;
                return AISEncoding.GetUnsigned(aisData.AISBytes, BitOffset, BitLength) * Scale + Offset;
            }
        }
    }

} // namespace
