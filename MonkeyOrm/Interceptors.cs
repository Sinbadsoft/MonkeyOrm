// <copyright file="Interceptors.cs" company="Sinbadsoft">
// Copyright (c) Chaker Nakhli 2010-2012
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at http://www.apache.org/licenses/LICENSE-2.0 Unless required by 
// applicable law or agreed to in writing, software distributed under the License
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,
// either express or implied. See the License for the specific language governing
// permissions and limitations under the License.
// </copyright>
// <author>Chaker Nakhli</author>
// <email>chaker.nakhli@sinbadsoft.com</email>
// <date>2012/06/10</date>
using System;

namespace MonkeyOrm
{
    internal class Interceptors : IInterceptors
    {
        private Func<object, object> unknownValueType = x => x;

        public Func<object, object> UnknownValueType
        {
            get
            {
                return this.unknownValueType;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", "This interceptor cannot be null");
                }

                this.unknownValueType = value;
            }
        }
    }
}