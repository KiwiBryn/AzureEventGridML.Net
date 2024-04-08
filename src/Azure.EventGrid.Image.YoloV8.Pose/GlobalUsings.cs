//---------------------------------------------------------------------------------
// Copyright (c) March 2024, devMobile Software - Azure Event Grid + YoloV8 for object detection PoC
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU
// Affero General Public License as published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. 
// If not, see <https://www.gnu.org/licenses/>
//
//---------------------------------------------------------------------------------
global using System.Net;
global using System.Text.Json;
global using System.Text;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

global using Compunet.YoloV8;
global using Compunet.YoloV8.Data;

global using HiveMQtt.Client;
global using HiveMQtt.MQTT5.ReasonCodes;
global using HiveMQtt.MQTT5.Types;


