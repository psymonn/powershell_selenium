// package com.test.MyServlets;
package org.openqa.grid.web.servlet;
 
import java.io.IOException;
import java.util.Iterator;
 
import javax.servlet.ServletException;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;
 
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;
import org.openqa.grid.common.exception.GridException;
import org.openqa.grid.internal.ProxySet;
import org.openqa.grid.internal.Registry;
import org.openqa.grid.internal.RemoteProxy;
import org.openqa.grid.web.servlet.RegistryBasedServlet;
 
public class MyConsoleServlet extends RegistryBasedServlet {
 
	/**
     * 
     */
    private static final long serialVersionUID = -1975392591408983229L;
    
    public MyConsoleServlet() {
    	this(null);
    }
 
	public MyConsoleServlet(Registry registry) {
		super(registry);
	}
 
	@Override
	protected void doGet(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
		doPost(req, resp);
	}
 
	@Override
	protected void doPost(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
		process(req, resp);
 
	}
 
	protected void process(HttpServletRequest request, HttpServletResponse response) throws IOException {
		response.setContentType("text/json");
		response.setCharacterEncoding("UTF-8");
		response.setStatus(200);
		JSONObject res;
	    try {
	      res = getResponse();
	      response.getWriter().print(res);
	      response.getWriter().close();
	    } catch (JSONException e) {
	      throw new GridException(e.getMessage());
	    }
 
	}
 
	private JSONObject getResponse() throws IOException, JSONException {
		JSONObject requestJSON = new JSONObject();
		ProxySet proxies = this.getRegistry().getAllProxies();
		Iterator<RemoteProxy> iterator = proxies.iterator();
		JSONArray busyProxies = new JSONArray();
		JSONArray freeProxies = new JSONArray();
		while (iterator.hasNext()) {
			RemoteProxy eachProxy = iterator.next();
			if (eachProxy.isBusy()) {
				busyProxies.put(eachProxy.getOriginalRegistrationRequest().getAssociatedJSON());
			} else {
				freeProxies.put(eachProxy.getOriginalRegistrationRequest().getAssociatedJSON());
			}
		}
		requestJSON.put("BusyProxies", busyProxies);
		requestJSON.put("FreeProxies", freeProxies);
 
		return requestJSON;
	}
 
}
/* Customizing the Grid
Adding custom servlets at the hub and/or node

The Grid lets you define your own servlets and then plug them into either the Hub (or) into the node. This lets you either add customizations at the hub side (or) at the node side. Lets take an example use case to understand this customization need a bit more in detail.

For the purpose of debugging, we would like to have access to the logs generated by all the nodes. The only problem is that we would need to enable logging into each of the machines where the nodes run. Instead of this we can build a custom solution by adding servlets at both the hub and the node.

This would be a very good use case for us to customize the Hub. We first start off by creating a custom servlet. All servlets (be it for the Hub or for the Node) can be created by either extending:

    org.openqa.grid.web.servlet.RegistryBasedServlet (or)
    javax.servlet.http.HttpServlet.

Extend org.openqa.grid.web.servlet.RegistryBasedServlet when you need access to the internals of the Hub (for e.g., its org.openqa.grid.internal.Registry which is the heart of the Hub) and extend javax.servlet.http.HttpServlet if you don’t need access to the Hub internals.

So for our example, lets say we are creating two servlets.

    A servlet ( lets call it as org.openqa.demo.AllNodes ) to be injected at the hub. When this servlet is invoked, it would list out all the nodes that are registered to the Hub and
    A servlet ( lets call it as org.openqa.demo.NodeLog ) to be injected into the node. When this servlet is invoked, it would read the logs from the node and serve it as a web page. For the sake of simplicity we are not going to be deliving into how to have the node redirect all its logging to a log file.

Now you need to build a jar file (lets assume its called myservlets.jar) that contains both the above mentioned classes (AllNodes and NodeLog).

From the directory where both myservlets.jar and selenium-server-standalone-2.38.0.jar exist, start the hub by running the command

java -cp *:. org.openqa.grid.selenium.GridLauncher -role hub -servlets org.openqa.demo.AllNodes

This command causes the Grid to be spawned and our new servlet gets added to the Hub. It can be accessed via http://localhost:4444/grid/admin/AllNodes

From the directory where both myservlets.jar and selenium-server-standalone-2.38.0.jar exist, start the node by running the command

java -cp *:. org.openqa.grid.selenium.GridLauncher -role node -hub http://localhost:4444/grid/register -servlets org.openqa.demo.AllNodes

This command causes the Node to be spawned and our new servlet gets added to the node. It can be accessed via http://xxx:5555/extra/NodeLog where xxx represents the machine name/ip where the node is running.




Points to remember:

    Assuming that the Hub is running on port 44444 all servlets added to the hub are accessible under http://xxx:4444/admin/ path and
    Assuming that the node is running on port 5555 all serlvets added to the node are accessible under http://xxx:5555/extra/ path.



http://paypal.github.io/SeLion/html/java-docs/server-apis/com/paypal/selion/grid/servlets/ListAllNodes.html

https://gist.github.com/krmahadevan/3302497
*/

