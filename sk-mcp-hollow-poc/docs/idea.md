# Example demo Agentic and MCP workflow

## Workflow-app Overview
A simple agentic and MCP driven workflow for developers to learn the foundational concepts of an agentic worflow with multiple types of deployments, such as local and in-process, and also with inter-process/remote commnunication with autonomous agents implemented as decopupled containers.

## Core value proposition
Easy to understand, review and evolve from local deployments to container-based deployments and even going further to a  cloud deployment.

## Target audience
Developers learning agentic frameworks and MCP.

## Functional Specifications
This is the area which is currently more vague. We could implement a specific but simple business idea, or we could implement a generic approach with no business meaning, but more like a template approach with names such as "Agent1", "Agent2", "MCPServer1", "MCPServer2"...

## Technical Specifications

*Multi-language implementation:* Some agents and MCP servers will be implemented in .NET while others will be implemented in Python.

*MCP standard used:* Using MCP (Model Context Protocol) not just for content sources but also for decoupled agents communication between them and the agents orchestrator.

*Agentic Frameworks:" Initially Semantic Kernel (from Microsoft) will be the core framework to use, as it supports .NET/C# and Python.
Eventually, a similar implementation could be done with AutoGen (also from Microsoft) in order to compare implementations, pros and cons from each.

*Local Agents and cloud Agents in Azure AI Foundry:Initially, in the baseline version, all the agents will be local, either on the local PC or in Docker containers. But there will be a second version which will have scalable agents deployed in Azure Foundary, still orchestrated by Semantic Kernel (or AutoGen). 

*Deployment modes:* As mentioned, the system will have several deployment modes depending on the potential needs:

- *A. Local agents and MCP servers:* Using stdio communication in MCP and starting the agents and MCP servers in a different way depending on the implementation language.

- *B. Decoupled container-based deployment (docker-compose):* Alternatively, an evolution of the example will be to containerized all the agents and MCP servers so it can be deployed in a Docker host with "docker compose". Communication between will be "remote" based on HTTP.

- *C. Kubernetes-based deployment:* Similar to the container-based implementation but the containers will be deployed into a local  Kubernetes cluster.

- *D. AKS (Azure Kubernetes Service based deployment):* Similar to the Kubernetes deployment but actually deploying into AKS in Azure, in addition to native agents deployed in zure AI Foundry.