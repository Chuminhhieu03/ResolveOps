export interface ShipmentLeg {
  id: string;
  sequenceNumber: number;
  carrierId: string;
  trackingNumber?: string;
  originLocationId: string;
  destinationLocationId: string;
  plannedDepartureAtUtc?: string;
  plannedArrivalAtUtc?: string;
  actualDepartureAtUtc?: string;
  actualArrivalAtUtc?: string;
  status: string;
}

export interface ShipmentItem {
  id: string;
  lineReference: string;
  sku?: string;
  description?: string;
  expectedQuantity: number;
  quantityUnit: string;
  unitValue?: number;
  currency?: string;
}

export interface ShipmentDetail {
  id: string;
  externalReference: string;
  sourceSystem: string;
  customerId: string;
  originLocationId: string;
  destinationLocationId: string;
  status: string;
  serviceLevel?: string;
  plannedPickupAtUtc: string;
  plannedDeliveryAtUtc: string;
  actualPickupAtUtc?: string;
  actualDeliveryAtUtc?: string;
  declaredValue?: number;
  declaredValueCurrency?: string;
  expectedPackageCount?: number;
  expectedWeight?: number;
  weightUnit?: string;
  legs: ShipmentLeg[];
  items: ShipmentItem[];
  createdAtUtc: string;
  updatedAtUtc?: string;
  concurrencyStamp: string;
}
