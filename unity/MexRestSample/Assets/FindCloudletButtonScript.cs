﻿using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using DistributedMatchEngine;

public class FindCloudletButtonScript : MonoBehaviour
{
  public Button findCloudletButton;
  MexSample mexSample;
  StatusContainer statusContainer;

  // Start is called before the first frame update
  void Start()
  {
    Button btn = findCloudletButton.GetComponent<Button>();
    btn.onClick.AddListener(TaskOnClick);

    statusContainer = GameObject.Find("/UICanvas/SampleOutput").GetComponent<StatusContainer>();
    mexSample = GameObject.Find("/UICanvas/MexSampleObject").GetComponent<MexSample>();
  }

  // Update is called once per frame
  void Update()
  {

  }

  async void TaskOnClick()
  {
    Debug.Log("FindCloudlet button!");
    var ok = await DoFindCloudlet();
  }

  async Task<bool> DoFindCloudlet()
  {
    bool ok = false;
    MatchingEngine dme = mexSample.dme;
    FindCloudletReply reply = null;

    try
    {
      var deviceSourcedLocation = await LocationService.RetrieveLocation();
      if (deviceSourcedLocation == null)
      {
        Debug.Log("FindCloudlet must have a device sourced location to send.");
        return false;
      }
      Debug.Log("Device sourced location: Lat: " + deviceSourcedLocation.latitude + "  Long: " + deviceSourcedLocation.longitude);

      var findCloudletRequest = dme.CreateFindCloudletRequest(
          dme.getCarrierName(),
          mexSample.devName,
          mexSample.appName,
          mexSample.appVers,
          deviceSourcedLocation);

      reply = await dme.FindCloudlet(mexSample.host, mexSample.port, findCloudletRequest);
      statusContainer.Post("FindCloudlet Reply status: " + reply.status);
    }
    catch (Exception e)
    {
      Console.WriteLine(e.StackTrace);
      statusContainer.Post("Exception: " + e.ToString());
      statusContainer.Post(e.StackTrace);
    }
    finally
    {
      if (reply != null)
      {
        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(FindCloudletReply));
        MemoryStream ms = new MemoryStream();
        serializer.WriteObject(ms, reply);
        string jsonStr = Util.StreamToString(ms);

        statusContainer.Post("GPS Cloudlet location: " + jsonStr + ", FQDN: " + reply.FQDN);

        // The list of registered edge cloudlets that the app can use:
        if (reply.ports.Length == 0)
        {
          statusContainer.Post("There are no app ports for this app's edge cloudlets.");
        }
        else
        {
          statusContainer.Post("Cloudlet app ports:");
          foreach (var appPort in reply.ports)
          {
            statusContainer.Post(
                    "Protocol: " + appPort.proto +
                    ", internal_port: " + appPort.internal_port +
                    ", public_port: " + appPort.public_port +
                    ", public_path: " + appPort.public_path +
                    ", FQDN_prefix: " + appPort.FQDN_prefix
            );
          }
        }
      }
    }
    return ok;
  }
}
